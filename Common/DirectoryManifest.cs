using System.Collections.Concurrent;

namespace Common;

public class DirectoryManifest
{
    public Dictionary<string, FileMetadata> Metadata { get; private init; } = new();
    
    private static readonly int MaxFileParallelism = Math.Min(Environment.ProcessorCount, 2);
    
    public static async Task<DirectoryManifest> From(string directoryPath, IProgress<double>? createManifestProgress = null, CancellationToken ct = default)
    {
        string[] files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
        
        var metadata = new ConcurrentDictionary<string, FileMetadata>();
        int completed = 0;

        await Parallel.ForEachAsync(
            files,
            new ParallelOptions
            {
                CancellationToken = ct,
                MaxDegreeOfParallelism = MaxFileParallelism
            },
            async (file, cancellationToken) =>
            {
                string relativeDir = Path.GetRelativePath(directoryPath, file);

                FileMetadata fileMetadata = await FileMetadata.From(file, cancellationToken);

                metadata[relativeDir] = fileMetadata;

                int done = Interlocked.Increment(ref completed);
                createManifestProgress?.Report((double)done / files.Length);
            });
        
        return new DirectoryManifest
        {
            Metadata = new Dictionary<string, FileMetadata>(metadata)
        };
    }
}

public static class DirectoryManifestExtensions
{
    extension(DirectoryManifest manifest)
    {
        public IEnumerable<string> Files => manifest.Metadata.Keys;
        public int FileCount => manifest.Metadata.Count;
    }
}
