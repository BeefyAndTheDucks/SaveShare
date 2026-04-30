using Octodiff.Core;
using Octodiff.Diagnostics;
using SharpCompress.Common;
using SharpCompress.Compressors.ZStandard;
using SharpCompress.Readers;
using SharpCompress.Writers;
using SharpCompress.Writers.Tar;

namespace Common;

public static class DirectoryPacker
{
    private const string k_SignatureFileExtension = ".octosig";
    private const string k_DeltaFileExtension = ".octodelta";
    
    private static string GetParentDirectory(string baseDirectory)
    {
        string directory = Path.GetDirectoryName(Path.GetFullPath(baseDirectory)) ?? throw new InvalidOperationException("Path must be a valid directory.");
        DirectoryInfo parentDirInfo = Directory.GetParent(directory)!;
        return parentDirInfo.FullName;
    }
    
    private static string GetSignatureDirectory(string baseDirectory) => Path.Combine(GetParentDirectory(baseDirectory), $".signatures_{Guid.NewGuid()}");
    private static string GetDeltasDirectory(string baseDirectory) => Path.Combine(GetParentDirectory(baseDirectory), $".deltas_{Guid.NewGuid()}");
    private static string GetUpdatedFilesDirectory(string baseDirectory) => Path.Combine(GetParentDirectory(baseDirectory), $".updated_{Guid.NewGuid()}");
    private static string GetUnpackDirectory(string baseDirectory) => Path.Combine(GetParentDirectory(baseDirectory), $".unpack_{Guid.NewGuid()}");
    private static string GetBackupDirectory(string baseDirectory) => Path.Combine(GetParentDirectory(baseDirectory), $".backup_{Guid.NewGuid()}");

    private static void SafeDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }
        catch
        {
            // ignored
        }
    }
    
    public static async Task<long> GetPackedSizeAsync(string path, CancellationToken ct = default)
    {
        await using CountingStream counter = new();
        await PackDirectoryAsync(path, counter, ct);
        return counter.Length;
    }
    
    public static async Task PackDirectoryAsync(string path, Stream output, CancellationToken ct = default)
    {
        await using CompressionStream compressionStream = new(output);
        await using IAsyncWriter writer = await WriterFactory.OpenAsyncWriter(compressionStream, ArchiveType.Tar,
            new TarWriterOptions(CompressionType.None), ct);
        await writer.WriteAllAsync(path, "*", SearchOption.AllDirectories, ct);
    }
    
    public static async Task UnpackDirectoryAsync(Stream input, string path, string? basePath = null, CancellationToken ct = default)
    {
        string tempUnpackPath = GetUnpackDirectory(path);
        string backupPath = GetBackupDirectory(path);
        
        Directory.CreateDirectory(tempUnpackPath);

        try
        {
            await using DecompressionStream decompressionStream = new(input);
            await using IAsyncReader reader = await ReaderFactory.OpenAsyncReader(decompressionStream, cancellationToken: ct);
            await reader.WriteAllToDirectoryAsync(tempUnpackPath, cancellationToken: ct);

            if (Directory.Exists(path))
            {
                Directory.Move(path, backupPath);
            }

            try
            {
                Directory.Move(tempUnpackPath, path);
            }
            catch
            {
                // Rollback: If moving the new one fails, put the old one back
                if (Directory.Exists(backupPath)) Directory.Move(backupPath, path);
                throw;
            }
        }
        finally
        {
            SafeDeleteDirectory(tempUnpackPath);
            SafeDeleteDirectory(backupPath);
        }
    }

    public static async Task BuildAndPackSignaturesAsync(string oldFilesPath, Func<Stream> output, IProgress<double>? createSignaturesProgress, Func<long, CancellationToken, Task>? onByteSizeCalculated = null, bool ownsStream = false, CancellationToken ct = default)
    {
        string signaturesPath = GetSignatureDirectory(oldFilesPath);
        Directory.CreateDirectory(signaturesPath);
        
        try
        {
            string[] files = Directory.GetFiles(oldFilesPath, "*", SearchOption.AllDirectories);
            SignatureBuilder signatureBuilder = new();
            for (int i = 0; i < files.Length; i++)
            {
                string relativeFilePath = Path.GetRelativePath(oldFilesPath, files[i]);
                string signatureFilePath = Path.Combine(signaturesPath, relativeFilePath + k_SignatureFileExtension);
                string? signatureOutputDirectory = Path.GetDirectoryName(signatureFilePath);
                if(!Directory.Exists(signatureOutputDirectory))
                    Directory.CreateDirectory(signatureOutputDirectory!);
                await using (FileStream basisStream = new(files[i], FileMode.Open, FileAccess.Read, FileShare.Read))
                await using (FileStream signatureStream = new(signatureFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    signatureBuilder.Build(basisStream, new SignatureWriter(signatureStream));
                }
            
                createSignaturesProgress?.Report(((double)i + 1) / files.Length);
            }
        
            if (onByteSizeCalculated != null)
                await onByteSizeCalculated.Invoke(await GetPackedSizeAsync(signaturesPath, ct), ct);
            Stream stream = output();
            await PackDirectoryAsync(signaturesPath, stream, ct);
            if (ownsStream)
                await stream.DisposeAsync();
        }
        finally
        {
            SafeDeleteDirectory(signaturesPath);
        }
    }

    public static async Task CreateDeltasAsync(string updatedFilesPath, Stream signaturesInput, Stream deltasOutput, IProgress<double>? createDeltasProgress, Func<long, CancellationToken, Task>? onByteSizeCalculated = null, CancellationToken ct = default)
    {
        string signaturesPath = GetSignatureDirectory(updatedFilesPath);
        string deltasPath = GetDeltasDirectory(updatedFilesPath);
        
        Directory.CreateDirectory(signaturesPath);
        Directory.CreateDirectory(deltasPath);

        try
        {
            await UnpackDirectoryAsync(signaturesInput, signaturesPath, updatedFilesPath, ct);

            string[] updatedFiles = Directory.GetFiles(updatedFilesPath, "*", SearchOption.AllDirectories);
            DeltaBuilder deltaBuilder = new();

            List<string> newFiles = [];

            int filesProcessed = 0;
            foreach (string updatedFile in updatedFiles)
            {
                string relativeFilePath = Path.GetRelativePath(updatedFilesPath, updatedFile);
                string signatureFilePath = Path.Combine(signaturesPath, relativeFilePath + k_SignatureFileExtension);
                string deltaFilePath = Path.Combine(deltasPath, relativeFilePath + k_DeltaFileExtension);
                string? deltaOutputDirectory = Path.GetDirectoryName(deltaFilePath);
                if (!Directory.Exists(deltaOutputDirectory) && deltaOutputDirectory != null)
                    Directory.CreateDirectory(deltaOutputDirectory);
                if (!File.Exists(signatureFilePath))
                {
                    newFiles.Add(updatedFile);
                    continue;
                }

                await using (FileStream newFileStream =
                             new(updatedFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                await using (FileStream signatureFileStream =
                             new(signatureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                await using (FileStream deltaStream = new(deltaFilePath, FileMode.Create, FileAccess.Write,
                                 FileShare.Read))
                {
                    deltaBuilder.BuildDelta(newFileStream, new SignatureReader(signatureFileStream, AbsoluteProgressReporter.From(createDeltasProgress, (double)filesProcessed / updatedFiles.Length, (double)(filesProcessed+1) / updatedFiles.Length)),
                        new AggregateCopyOperationsDecorator(new BinaryDeltaWriter(deltaStream)));
                }

                filesProcessed++;
                createDeltasProgress?.Report((double)filesProcessed / updatedFiles.Length);
            }

            foreach (string newFile in newFiles)
            {
                string relativePath = Path.GetRelativePath(updatedFilesPath, newFile);
                string destinationPath = Path.Combine(deltasPath, relativePath);
                string? destDirectory = Path.GetDirectoryName(destinationPath);
                if (!Directory.Exists(destDirectory) && destDirectory != null)
                    Directory.CreateDirectory(destDirectory);
                File.Copy(newFile, destinationPath, true);
                
                filesProcessed++;
                createDeltasProgress?.Report((double)filesProcessed / updatedFiles.Length);
            }

            if (onByteSizeCalculated != null)
                await onByteSizeCalculated.Invoke(await GetPackedSizeAsync(deltasPath, ct), ct);
            await PackDirectoryAsync(deltasPath, deltasOutput, ct);
        }
        finally
        {
            SafeDeleteDirectory(signaturesPath);
            SafeDeleteDirectory(deltasPath);
        }
    }

    public static async Task ApplyDeltasAsync(string oldFilesPath, Stream deltasInput, IProgress<double>? applyDeltasProgress, CancellationToken ct = default)
    {
        string deltasPath = GetDeltasDirectory(oldFilesPath);
        string updatedFilesPath = GetUpdatedFilesDirectory(oldFilesPath);
        string backupPath = GetBackupDirectory(oldFilesPath);

        Directory.CreateDirectory(deltasPath);
        Directory.CreateDirectory(updatedFilesPath);

        try
        {
            await UnpackDirectoryAsync(deltasInput, deltasPath, oldFilesPath, ct);
            
            string[] deltaFiles = Directory.GetFiles(deltasPath, "*", SearchOption.AllDirectories);
            for (int i = 0; i < deltaFiles.Length; i++)
            {
                string relativePath = Path.GetRelativePath(deltasPath, deltaFiles[i]);
                string dir = Path.GetDirectoryName(relativePath) ?? "";
                string name = Path.GetFileNameWithoutExtension(relativePath);
                string destinationPath = Path.Combine(updatedFilesPath, dir, name);
                string oldFilePath = Path.Combine(oldFilesPath, dir, name);

                if (!File.Exists(oldFilePath))
                {
                    string destinationFile = Path.Combine(updatedFilesPath, relativePath);
                    string directoryName = Path.GetDirectoryName(destinationFile) ?? "";
                    if (!Directory.Exists(directoryName))
                        Directory.CreateDirectory(directoryName);
                    
                    File.Copy(deltaFiles[i], destinationFile, true);
                    continue;
                }
                
                string? updatedFileOutputDirectory = Path.GetDirectoryName(destinationPath);
                if (!Directory.Exists(updatedFileOutputDirectory) && updatedFileOutputDirectory != null)
                    Directory.CreateDirectory(updatedFileOutputDirectory);
                DeltaApplier deltaApplier = new();
                await using(FileStream basisStream = new(oldFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                await using(FileStream deltaStream = new(deltaFiles[i], FileMode.Open, FileAccess.Read, FileShare.Read))
                await using(FileStream newFileStream = new(destinationPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
                {
                    deltaApplier.Apply(basisStream, new BinaryDeltaReader(deltaStream, AbsoluteProgressReporter.From(applyDeltasProgress, (double)i / deltaFiles.Length, (double)(i+1) / deltaFiles.Length)), newFileStream);
                }
                
                applyDeltasProgress?.Report(((double)i + 1) / deltaFiles.Length);
            }
            
            Directory.Move(oldFilesPath, backupPath);

            try
            {
                Directory.Move(updatedFilesPath, oldFilesPath);
            }
            catch
            {
                // Rollback: If moving the new one fails, put the old one back
                if (Directory.Exists(backupPath)) Directory.Move(backupPath, oldFilesPath);
                throw;
            }
        }
        finally
        {
            SafeDeleteDirectory(deltasPath);
            SafeDeleteDirectory(updatedFilesPath);
            SafeDeleteDirectory(backupPath);
        }
    }
}
