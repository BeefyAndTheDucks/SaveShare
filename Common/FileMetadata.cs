using System.IO.Hashing;

namespace Common;

public class FileMetadata
{
    public ulong Hash { get; set; }
    
    public static async Task<FileMetadata> From(string path, CancellationToken ct = default)
    {
        XxHash3 hasher = new();

        FileStreamOptions options = new()
        {
            Mode = FileMode.Open,
            Access = FileAccess.Read,
            Share = FileShare.Read,
            BufferSize = 1024 * 1024,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
        };
        
        await using (FileStream fs = File.Open(path, options))
        {
            await hasher.AppendAsync(fs, ct);
        }

        ulong hash = hasher.GetCurrentHashAsUInt64();
            
        return new FileMetadata
        {
            Hash = hash,
        };
    }
}