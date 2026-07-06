using System.Diagnostics.CodeAnalysis;
using Octodiff.Core;
using Octodiff.Diagnostics;
using SharpCompress.Common;
using SharpCompress.Compressors.ZStandard;
using SharpCompress.Readers;
using SharpCompress.Writers;
using SharpCompress.Writers.Tar;

namespace Common;

public static class SavePacker
{
    internal const string SINGLE_FILE_SAVE_FILE_FILE_NAME = "ROOT";
    
    private const string k_SignatureFileExtension = ".octosig";
    private const string k_DeltaFileExtension = ".octodelta";

    private const char k_File = 'F';
    private const char k_Directory = 'D';
    
    private const int k_HeaderSize = 1;
    
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
    
    private static string GetSignatureFile(string file, string? basePath = null) => Path.Combine(basePath ?? Path.GetDirectoryName(file) ?? throw new InvalidOperationException("Path must be valid."), $".signatures_{Guid.NewGuid()}");
    private static string GetDeltasFile(string file, string? basePath = null) => Path.Combine(basePath ?? Path.GetDirectoryName(file) ?? throw new InvalidOperationException("Path must be valid."), $".deltas_{Guid.NewGuid()}");
    private static string GetUpdatedFilesFile(string file, string? basePath = null) => Path.Combine(basePath ?? Path.GetDirectoryName(file) ?? throw new InvalidOperationException("Path must be valid."), $".updated_{Guid.NewGuid()}");
    private static string GetUnpackFile(string file, string? basePath = null) => Path.Combine(basePath ?? Path.GetDirectoryName(file) ?? throw new InvalidOperationException("Path must be valid."), $".unpack_{Guid.NewGuid()}");
    private static string GetBackupFile(string file, string? basePath = null) => Path.Combine(basePath ?? Path.GetDirectoryName(file) ?? throw new InvalidOperationException("Path must be valid."), $".backup_{Guid.NewGuid()}");

    private static void SafeDeleteDirectory(string path)
    {
        try
        {
            Directory.Delete(path, true);
        }
        catch
        {
            // ignored
        }
    }
    
    private static void SafeDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to delete file: \"{path}\": {ex}");
        }
    }

    [SuppressMessage("ReSharper", "RedundantAssignment")]
    private static async Task WriteHeader(Stream stream, PackedHeader header, CancellationToken cancellationToken = default)
    {
        byte[] buffer = new byte[k_HeaderSize];
        int pointer = 0;
        
        buffer[pointer] = Convert.ToByte(header.Type);
        pointer += 1;
        
        await stream.WriteAsync(buffer, cancellationToken);
    }

    [SuppressMessage("ReSharper", "RedundantAssignment")]
    private static async Task<PackedHeader> ReadHeader(Stream stream, CancellationToken cancellationToken = default)
    {
        byte[] buffer = new byte[k_HeaderSize];
        int pointer = 0;
        await stream.ReadExactlyAsync(buffer, cancellationToken);
        
        char type = Convert.ToChar(buffer[pointer]);
        pointer += 1;

        return new PackedHeader(type);
    }
    
    public static async Task PackAsync(string path, Func<Stream> output, bool ownsStream, Func<long, CancellationToken, Task>? gotPackedSize = null, CancellationToken ct = default)
    {
        string tempFile = Path.GetTempFileName();
        await using FileStream temporaryFileStream = new(tempFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        try
        {
            char type;
            await using (CompressionStream compressionStream = new(temporaryFileStream))
            {
                if (File.Exists(path))
                {
                    type = k_File;
                    await PackFileAsync(path, compressionStream, ct);
                }
                else if (Directory.Exists(path))
                {
                    type = k_Directory;
                    await PackDirectoryAsync(path, compressionStream, ct);
                }
                else
                {
                    throw new ArgumentException($"Path must be a file or directory. (got {path})", nameof(path));
                }
            }

            temporaryFileStream.Seek(0, SeekOrigin.Begin);
            long length = temporaryFileStream.Length + k_HeaderSize;
            if (gotPackedSize != null)
                await gotPackedSize.Invoke(length, ct);
            Stream stream = output();
            
            WriteHeader(stream, new PackedHeader(type), ct).GetAwaiter().GetResult();
            
            await temporaryFileStream.CopyToAsync(stream, ct);
            if (ownsStream)
                await stream.DisposeAsync();
        }
        finally
        {
            SafeDeleteFile(tempFile);
        }
    }
    
    public static async Task PackAsync(string path, Stream output, bool ownsStream, Func<long, CancellationToken, Task>? gotPackedSize = null, CancellationToken ct = default)
    {
        await PackAsync(path, () => output, ownsStream, gotPackedSize, ct);
    }

    private static async Task PackFileAsync(string path, Stream output, CancellationToken ct = default)
    {
        await using IAsyncWriter writer = await WriterFactory.OpenAsyncWriter(output, ArchiveType.Tar,
            new TarWriterOptions(CompressionType.None), ct);
        await using FileStream fileStream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        await writer.WriteAsync(SINGLE_FILE_SAVE_FILE_FILE_NAME, fileStream, ct);
    }

    private static async Task PackDirectoryAsync(string path, Stream output, CancellationToken ct = default)
    {
        await using IAsyncWriter writer = await WriterFactory.OpenAsyncWriter(output, ArchiveType.Tar,
            new TarWriterOptions(CompressionType.None), ct);
        await writer.WriteAllAsync(path, "*", SearchOption.AllDirectories, ct);
    }

    public static async Task UnpackAsync(Stream input, string path, string? basePath = null,
        CancellationToken ct = default)
    {
        PackedHeader header = await ReadHeader(input, ct);

        switch (header.Type)
        {
            case k_File:
                await UnpackFileAsync(input, path, basePath, ct);
                return;
            case k_Directory:
                await UnpackDirectoryAsync(input, path, basePath, ct);
                return;
            default:
                throw new InvalidOperationException($"Invalid packed type: {header.Type}");
        }
    }
    
    private static async Task UnpackDirectoryAsync(Stream input, string path, string? basePath = null, CancellationToken ct = default)
    {
        string tempUnpackPath = GetUnpackDirectory(basePath ?? path);
        string backupPath = GetBackupDirectory(basePath ?? path);
        
        Directory.CreateDirectory(tempUnpackPath);
        Directory.CreateDirectory(path);

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

    private static async Task UnpackFileAsync(Stream input, string path, string? basePath = null, CancellationToken ct = default)
    {
        string unpackPath = GetUnpackFile(path, basePath);
        string backupPath = GetBackupFile(path, basePath);
        
        try
        {
            await using (DecompressionStream decompressionStream = new(input))
            await using (IAsyncReader reader =
                         await ReaderFactory.OpenAsyncReader(decompressionStream, cancellationToken: ct))
            {
                if (await reader.MoveToNextEntryAsync(ct))
                {
                    await using FileStream unpackFileStream = new(unpackPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await reader.WriteEntryToAsync(unpackFileStream, ct);
                }
                else
                {
                    throw new InvalidDataException("No file found in archive.");
                }
            }
            
            if (File.Exists(path))
            {
                File.Move(path, backupPath);
            }

            try
            {
                File.Move(unpackPath, path);
            }
            catch
            {
                // Rollback: If moving the new one fails, put the old one back
                if (File.Exists(backupPath)) File.Move(backupPath, path);
                throw;
            }
        }
        finally
        {
            SafeDeleteFile(unpackPath);
            SafeDeleteFile(backupPath);
        }
    }
    
    public static async Task BuildAndPackSignaturesAsync(string oldFilesPath, Func<Stream> output,
        SaveManifest newFilesManifest, SaveManifest oldFilesManifest, IProgress<double>? createSignaturesProgress,
        Func<long, CancellationToken, Task>? onByteSizeCalculated = null, bool ownsStream = false,
        CancellationToken ct = default)
    {
        if (File.Exists(oldFilesPath))
            await BuildAndPackSignaturesFileAsync(oldFilesPath, output, createSignaturesProgress, onByteSizeCalculated, ownsStream, ct);
        else if (Directory.Exists(oldFilesPath))
            await BuildAndPackSignaturesDirectoryAsync(oldFilesPath, output, newFilesManifest, oldFilesManifest, createSignaturesProgress, onByteSizeCalculated, ownsStream, ct);
        else
            throw new ArgumentException($"Path must be a file or directory. (got {oldFilesPath})", nameof(oldFilesPath));
    }

    private static async Task BuildAndPackSignaturesDirectoryAsync(string oldFilesPath, Func<Stream> output,
        SaveManifest newFilesManifest, SaveManifest oldFilesManifest, IProgress<double>? createSignaturesProgress,
        Func<long, CancellationToken, Task>? onByteSizeCalculated = null, bool ownsStream = false,
        CancellationToken ct = default)
    {
        string signaturesPath = GetSignatureDirectory(oldFilesPath);
        Directory.CreateDirectory(signaturesPath);
        
        try
        {
            string[] changedFiles = Directory.EnumerateFiles(oldFilesPath, "*", SearchOption.AllDirectories)
                .Where(filePath =>
                {
                    string relativeFilePath = Path.GetRelativePath(oldFilesPath, filePath);
                    FileMetadata oldFileMetadata = oldFilesManifest.Metadata[relativeFilePath];
                    if (!newFilesManifest.Metadata.TryGetValue(relativeFilePath, out FileMetadata? newFileMetadata))
                        return false;
                    
                    return oldFileMetadata.Hash != newFileMetadata.Hash;
                })
                .ToArray();
            
            if (changedFiles.Length == 0)
                createSignaturesProgress?.Report(1);
            else
            {
                int filesProcessed = 0;
                await Parallel.ForEachAsync(
                    changedFiles,
                    new ParallelOptions
                    {
                        CancellationToken = ct,
                        MaxDegreeOfParallelism = Constants.MaxFileParallelism
                    },
                    async (changedFile, cancellationToken) => 
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        string relativeFilePath = Path.GetRelativePath(oldFilesPath, changedFile);
                        FileMetadata localFileMetadata = oldFilesManifest.Metadata[relativeFilePath];
                        bool existsOnOtherEnd = newFilesManifest.Metadata.TryGetValue(relativeFilePath, out FileMetadata? otherEndMetadata);
                        
                        if (existsOnOtherEnd && otherEndMetadata!.Hash != localFileMetadata.Hash)
                        {
                            string signatureFilePath = Path.Combine(signaturesPath, relativeFilePath + k_SignatureFileExtension);
                            string? signatureOutputDirectory = Path.GetDirectoryName(signatureFilePath);
                            if (signatureOutputDirectory != null)
                                Directory.CreateDirectory(signatureOutputDirectory);
                            
                            SignatureBuilder signatureBuilder = new();
                            
                            // ReSharper disable once ConvertToUsingDeclaration
                            await using (FileStream basisStream = new(changedFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                            await using (FileStream signatureStream = new(signatureFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                            {
                                signatureBuilder.Build(basisStream, new SignatureWriter(signatureStream));
                            }
                        }
                
                        int completed = Interlocked.Increment(ref filesProcessed);
                        createSignaturesProgress?.Report((double)completed / changedFiles.Length);
                    });
            }
        
            await PackAsync(signaturesPath, output, ownsStream, onByteSizeCalculated, ct);
        }
        finally
        {
            SafeDeleteDirectory(signaturesPath);
        }
    }

    private static async Task BuildAndPackSignaturesFileAsync(string oldFilesPath, Func<Stream> output,
        IProgress<double>? createSignaturesProgress, Func<long, CancellationToken, Task>? onByteSizeCalculated = null,
        bool ownsStream = false, CancellationToken ct = default)
    {
        string signaturePath = GetSignatureFile(oldFilesPath);

        try
        {
            SignatureBuilder signatureBuilder = new()
            {
                ProgressReporter = ProgressReporterToIProgress.From(createSignaturesProgress)
            };

            await using (FileStream basisStream = new(oldFilesPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            await using (FileStream signatureStream =
                         new(signaturePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                await Task.Run(() =>
                {
                    signatureBuilder.Build(basisStream, new SignatureWriter(signatureStream));
                }, ct);
            }
            
            createSignaturesProgress?.Report(1);
            await PackAsync(signaturePath, output, ownsStream, onByteSizeCalculated, ct);
        }
        finally
        {
            SafeDeleteFile(signaturePath);
        }
    }

    public static async Task CreateDeltasAsync(string updatedFilesPath, Stream signaturesInput, Stream deltasOutput,
        SaveManifest newFilesManifest, SaveManifest oldFilesManifest, IProgress<double>? createDeltasProgress,
        Func<long, CancellationToken, Task>? onByteSizeCalculated = null, CancellationToken ct = default)
    {
        if (File.Exists(updatedFilesPath))
            await CreateDeltasFileAsync(updatedFilesPath, signaturesInput, deltasOutput, createDeltasProgress, onByteSizeCalculated, ct);
        else if (Directory.Exists(updatedFilesPath))
            await CreateDeltasDirectoryAsync(updatedFilesPath, signaturesInput, deltasOutput, newFilesManifest, oldFilesManifest, createDeltasProgress, onByteSizeCalculated, ct);
        else
            throw new ArgumentException($"Path must be a file or directory. (got {updatedFilesPath})", nameof(updatedFilesPath));
    }
    
    private static async Task CreateDeltasFileAsync(string updatedFilePath, Stream signaturesInput, Stream deltasOutput,
        IProgress<double>? createDeltasProgress, Func<long, CancellationToken, Task>? onByteSizeCalculated = null,
        CancellationToken ct = default)
    {
        string signaturePath = GetSignatureFile(updatedFilePath);
        string deltaPath = GetDeltasFile(updatedFilePath);

        try
        {
            await UnpackAsync(signaturesInput, signaturePath, Path.GetDirectoryName(updatedFilePath) ?? throw new InvalidOperationException(), ct);

            DeltaBuilder deltaBuilder = new()
            {
                ProgressReporter = ProgressReporterToIProgress.From(createDeltasProgress)
            };
            
            await using(FileStream newFileStream = new(updatedFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            await using(FileStream signatureFileStream = new(signaturePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            await using(FileStream deltaStream = new(deltaPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                await Task.Run(() =>
                {
                    deltaBuilder.BuildDelta(newFileStream,
                        new SignatureReader(signatureFileStream, NullProgressReporter.Instance),
                        new AggregateCopyOperationsDecorator(new BinaryDeltaWriter(deltaStream)));
                }, ct);
            }
            
            createDeltasProgress?.Report(1);
            
            await PackAsync(deltaPath, deltasOutput, false, onByteSizeCalculated, ct);
        }
        finally
        {
            SafeDeleteFile(signaturePath);
            SafeDeleteFile(deltaPath);
        }
    }

    private static async Task CreateDeltasDirectoryAsync(string updatedFilesPath, Stream signaturesInput,
        Stream deltasOutput, SaveManifest newFilesManifest, SaveManifest oldFilesManifest,
        IProgress<double>? createDeltasProgress, Func<long, CancellationToken, Task>? onByteSizeCalculated = null,
        CancellationToken ct = default)
    {
        string signaturesPath = GetSignatureDirectory(updatedFilesPath);
        string deltasPath = GetDeltasDirectory(updatedFilesPath);
        
        Directory.CreateDirectory(signaturesPath);
        Directory.CreateDirectory(deltasPath);

        try
        {
            await UnpackAsync(signaturesInput, signaturesPath, updatedFilesPath, ct);

            string[] updatedFiles = Directory.EnumerateFiles(updatedFilesPath, "*", SearchOption.AllDirectories)
                .Where(filePath =>
                {
                    string relativeFilePath = Path.GetRelativePath(updatedFilesPath, filePath);
                    FileMetadata newFileMetadata = newFilesManifest.Metadata[relativeFilePath];
                    if (!oldFilesManifest.Metadata.TryGetValue(relativeFilePath, out FileMetadata? oldFileMetadata))
                        return true;
                    
                    return newFileMetadata.Hash != oldFileMetadata.Hash;
                })
                .ToArray();

            int filesProcessed = 0;
            
            if (updatedFiles.Length == 0)
                createDeltasProgress?.Report(1);
            else
            {
                await Parallel.ForEachAsync(
                    updatedFiles,
                    new ParallelOptions
                    {
                        CancellationToken = ct,
                        MaxDegreeOfParallelism = Constants.MaxFileParallelism
                    },
                    async (updatedFile, cancellationToken) =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        string relativeFilePath = Path.GetRelativePath(updatedFilesPath, updatedFile);
                        string signatureFilePath = Path.Combine(signaturesPath, relativeFilePath + k_SignatureFileExtension);
                        
                        if (!File.Exists(signatureFilePath))
                        {
                            string destinationPath = Path.Combine(deltasPath, relativeFilePath);
                            string? destinationDirectory = Path.GetDirectoryName(destinationPath);
                            if (destinationDirectory != null)
                                Directory.CreateDirectory(destinationDirectory);

                            File.Copy(updatedFile, destinationPath, true);
                        }
                        else
                        {
                            string deltaFilePath = Path.Combine(deltasPath, relativeFilePath + k_DeltaFileExtension);
                            string? deltaOutputDirectory = Path.GetDirectoryName(deltaFilePath);
                            if (deltaOutputDirectory != null)
                                Directory.CreateDirectory(deltaOutputDirectory);

                            DeltaBuilder deltaBuilder = new();

                            await using FileStream newFileStream = new(updatedFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                            await using FileStream signatureFileStream = new(signatureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                            await using FileStream deltaStream = new(deltaFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);

                            deltaBuilder.BuildDelta(
                                newFileStream,
                                new SignatureReader(signatureFileStream, NullProgressReporter.Instance),
                                new AggregateCopyOperationsDecorator(new BinaryDeltaWriter(deltaStream)));
                        }
                        
                        int completed = Interlocked.Increment(ref filesProcessed);
                        createDeltasProgress?.Report((double)completed / updatedFiles.Length);
                    });
            }

            await PackAsync(deltasPath, deltasOutput, false, onByteSizeCalculated, ct);
        }
        finally
        {
            SafeDeleteDirectory(signaturesPath);
            SafeDeleteDirectory(deltasPath);
        }
    }
    
    public static async Task ApplyDeltasAsync(string oldFilesPath, Stream deltasInput, SaveManifest newFilesManifest,
        SaveManifest oldFilesManifest, IProgress<double>? applyDeltasProgress, CancellationToken ct = default)
    {
        if (File.Exists(oldFilesPath))
            await ApplyDeltasFileAsync(oldFilesPath, deltasInput, applyDeltasProgress, ct);
        else if (Directory.Exists(oldFilesPath))
            await ApplyDeltasDirectoryAsync(oldFilesPath, deltasInput, newFilesManifest, oldFilesManifest, applyDeltasProgress, ct);
        else
            throw new ArgumentException($"Path must be a file or directory. (got {oldFilesPath})", nameof(oldFilesPath));
    }
    
    private static async Task ApplyDeltasFileAsync(string oldFilePath, Stream deltasInput,
        IProgress<double>? applyDeltasProgress, CancellationToken ct = default)
    {
        string deltaPath = GetDeltasFile(oldFilePath);
        string updatedFilePath = GetUpdatedFilesFile(oldFilePath);
        string backupPath = GetBackupFile(oldFilePath);

        try
        {
            await UnpackAsync(deltasInput, deltaPath, Path.GetDirectoryName(oldFilePath) ?? throw new InvalidOperationException(), ct);
            
            DeltaApplier deltaApplier = new() { SkipHashCheck = false };
            await using(FileStream basisStream = new(oldFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            await using(FileStream deltaStream = new(deltaPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            await using(FileStream newFileStream = new(updatedFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
            {
                await Task.Run(() =>
                {
                    deltaApplier.Apply(basisStream,
                        new BinaryDeltaReader(deltaStream, ProgressReporterToIProgress.From(applyDeltasProgress)),
                        newFileStream);
                }, ct);
            }
            
            File.Move(oldFilePath, backupPath);

            try
            {
                File.Move(updatedFilePath, oldFilePath);
            }
            catch
            {
                // Rollback: If moving the new one fails, put the old one back
                if (File.Exists(backupPath)) File.Move(backupPath, oldFilePath);
                throw;
            }
            
            applyDeltasProgress?.Report(1);
        }
        finally
        {
            SafeDeleteFile(deltaPath);
            SafeDeleteFile(updatedFilePath);
            SafeDeleteFile(backupPath);
        }
    }
    
    private static async Task ApplyDeltasDirectoryAsync(string oldFilesPath, Stream deltasInput,
        SaveManifest newFilesManifest, SaveManifest oldFilesManifest, IProgress<double>? applyDeltasProgress,
        CancellationToken ct = default)
    {
        string deltasPath = GetDeltasDirectory(oldFilesPath);
        string updatedFilesPath = GetUpdatedFilesDirectory(oldFilesPath);
        string backupPath = GetBackupDirectory(oldFilesPath);

        Directory.CreateDirectory(deltasPath);
        Directory.CreateDirectory(updatedFilesPath);

        try
        {
            await UnpackAsync(deltasInput, deltasPath, oldFilesPath, ct);
            
            string[] deltaFiles = Directory.GetFiles(deltasPath, "*", SearchOption.AllDirectories);
            
            string[] nonModifiedFiles = newFilesManifest.Metadata
                .Where(kv =>
                {
                    string relativeFilePath = kv.Key;
                    FileMetadata newFileMetadata = kv.Value;
                    if (!oldFilesManifest.Metadata.TryGetValue(relativeFilePath, out FileMetadata? oldFileMetadata))
                        return false;

                    return newFileMetadata.Hash == oldFileMetadata.Hash;
                })
                .Select(kv => kv.Key)
                .ToArray();
            
            int filesToProcess = deltaFiles.Length + nonModifiedFiles.Length;
            int filesProcessed = 0;
            
            if (filesToProcess == 0)
                applyDeltasProgress?.Report(1);
            else
            {
                await Parallel.ForEachAsync(
                    deltaFiles,
                    new ParallelOptions
                    {
                        CancellationToken = ct,
                        MaxDegreeOfParallelism = Constants.MaxFileParallelism
                    },
                    async (deltaFile, cancellationToken) =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        string relativePath = Path.GetRelativePath(deltasPath, deltaFile);
                        string dir = Path.GetDirectoryName(relativePath) ?? "";
                        string name = Path.GetFileNameWithoutExtension(relativePath);
                        string destinationPath = Path.Combine(updatedFilesPath, dir, name);
                        string oldFilePath = Path.Combine(oldFilesPath, dir, name);

                        if (!File.Exists(oldFilePath))
                        {
                            string destinationFile = Path.Combine(updatedFilesPath, relativePath);
                            string? directoryName = Path.GetDirectoryName(destinationFile);
                            if (directoryName != null)
                                Directory.CreateDirectory(directoryName);

                            File.Copy(deltaFile, destinationFile, true);
                        }
                        else
                        {
                            string? updatedFileOutputDirectory = Path.GetDirectoryName(destinationPath);
                            if (updatedFileOutputDirectory != null)
                                Directory.CreateDirectory(updatedFileOutputDirectory);
                            
                            DeltaApplier deltaApplier = new();

                            await using FileStream basisStream = new(oldFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                            await using FileStream deltaStream = new(deltaFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                            await using FileStream newFileStream = new(destinationPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

                            deltaApplier.Apply(
                                basisStream,
                                new BinaryDeltaReader(deltaStream, NullProgressReporter.Instance),
                                newFileStream);
                        }
                        
                        int completed = Interlocked.Increment(ref filesProcessed);
                        applyDeltasProgress?.Report((double)completed / filesToProcess);
                    });
            }

            await Parallel.ForEachAsync(
                nonModifiedFiles,
                new ParallelOptions
                {
                    CancellationToken = ct,
                    MaxDegreeOfParallelism = Constants.MaxFileParallelism
                },
                (relativePath, cancellationToken) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string sourcePath = Path.Combine(oldFilesPath, relativePath);
                    string destinationPath = Path.Combine(updatedFilesPath, relativePath);
                    string? destinationDirectory = Path.GetDirectoryName(destinationPath);
                    if (destinationDirectory != null)
                        Directory.CreateDirectory(destinationDirectory);

                    File.Copy(sourcePath, destinationPath, true);

                    int completed = Interlocked.Increment(ref filesProcessed);
                    applyDeltasProgress?.Report((double)completed / filesToProcess);

                    return ValueTask.CompletedTask;
                });
            
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

    private record PackedHeader(char Type);
}