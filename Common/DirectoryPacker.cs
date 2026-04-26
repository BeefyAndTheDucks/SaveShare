using SharpCompress.Common;
using SharpCompress.Compressors.ZStandard;
using SharpCompress.Readers;
using SharpCompress.Writers;
using SharpCompress.Writers.Tar;

namespace Common;

public static class DirectoryPacker
{
    public static async Task<long> GetPackedSize(string path, CancellationToken ct = default)
    {
        await using CountingStream counter = new();
        await PackDirectory(path, counter, ct);
        return counter.Length;
    }
    
    public static async Task PackDirectory(string path, Stream output, CancellationToken ct = default)
    {
        await using CompressionStream compressionStream = new(output);
        await using IAsyncWriter writer = await WriterFactory.OpenAsyncWriter(compressionStream, ArchiveType.Tar,
            new TarWriterOptions(CompressionType.None), ct);
        await writer.WriteAllAsync(path, "*", SearchOption.AllDirectories, ct);
    }
    
    public static async Task UnpackDirectory(Stream input, string path, CancellationToken ct = default)
    {
        string directoryDir = Path.GetDirectoryName(Path.GetFullPath(path)) ?? throw new InvalidOperationException("Path must be a valid directory.");
        DirectoryInfo parentDirInfo = Directory.GetParent(directoryDir)!;
        string parentDir = parentDirInfo.FullName;
        
        string tempUnpackPath = Path.Combine(parentDir, $".unpack_{Guid.NewGuid()}");
        string backupPath = Path.Combine(parentDir, $".backup_{Guid.NewGuid()}");
        
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
            if (Directory.Exists(tempUnpackPath)) Directory.Delete(tempUnpackPath, true);
            if (Directory.Exists(backupPath)) Directory.Delete(backupPath, true);
        }
    }
}