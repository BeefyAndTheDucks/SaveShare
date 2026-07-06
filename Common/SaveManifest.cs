using System.Collections.Concurrent;

namespace Common;

public class SaveManifest
{
    public Dictionary<string, FileMetadata> Metadata { get; private init; } = new();
    
    public static async Task<SaveManifest> From(string directoryPath, IProgress<double>? createManifestProgress = null, CancellationToken ct = default)
    {
        if (File.Exists(directoryPath))
        {
            SaveManifest manifest = await FromSingleFile(directoryPath, ct);
            createManifestProgress?.Report(1);
            return manifest;
        }
        
        string[] files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
        
        var metadata = new ConcurrentDictionary<string, FileMetadata>();
        int completed = 0;

        await Parallel.ForEachAsync(
            files,
            new ParallelOptions
            {
                CancellationToken = ct,
                MaxDegreeOfParallelism = Constants.MaxFileParallelism
            },
            async (file, cancellationToken) =>
            {
                string relativeDir = Path.GetRelativePath(directoryPath, file);

                FileMetadata fileMetadata = await FileMetadata.From(file, cancellationToken);

                metadata[relativeDir] = fileMetadata;

                int done = Interlocked.Increment(ref completed);
                createManifestProgress?.Report((double)done / files.Length);
            });
        
        return new SaveManifest
        {
            Metadata = new Dictionary<string, FileMetadata>(metadata)
        };
    }

    public static async Task<SaveManifest> FromSingleFile(string filePath, CancellationToken ct = default)
    {
        return FromSingleFile(await FileMetadata.From(filePath, ct));
    }

    public static SaveManifest FromSingleFile(FileMetadata file)
    {
        return new SaveManifest
        {
            Metadata = new Dictionary<string, FileMetadata> { { SavePacker.SINGLE_FILE_SAVE_FILE_FILE_NAME, file } }
        };
    }
}

public static class DirectoryManifestExtensions
{
    extension(SaveManifest manifest)
    {
        public IEnumerable<string> Files => manifest.Metadata.Keys;
        public int FileCount => manifest.Metadata.Count;
    }
}
