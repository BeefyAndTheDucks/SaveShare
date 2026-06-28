using System.IO.Hashing;

namespace Common;

public class FileMetadata
{
    public ulong Hash { get; set; }
    
    public static async Task<FileMetadata> From(string path, CancellationToken ct = default)
    {
        XxHash3 hasher = new();

        await using (FileStream fs = File.OpenRead(path))
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