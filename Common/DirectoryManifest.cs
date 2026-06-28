namespace Common;

public class DirectoryManifest
{
    public Dictionary<string, FileMetadata> Metadata { get; set; } = new();
    
    public static async Task<DirectoryManifest> From(string directoryPath, IProgress<double>? createManifestProgress = null, CancellationToken ct = default)
    {
        DirectoryManifest manifest = new();
        
        string[] files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
        
        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i];
            string relativeDir = Path.GetRelativePath(directoryPath, file);

            FileMetadata metadata = await FileMetadata.From(file, ct);
            
            manifest.Metadata[relativeDir] = metadata;
            createManifestProgress?.Report(((double)i + 1) / files.Length);
        }
        
        return manifest;
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