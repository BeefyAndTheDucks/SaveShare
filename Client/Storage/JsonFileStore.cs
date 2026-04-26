using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Client.Interfaces;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Client.Storage;

public sealed class JsonFileStore : IFileStore
{
    private static readonly JsonSerializerSettings JsonOptions = new()
    {
        Formatting = Formatting.Indented,
    };
    
    public async Task<T?> ReadAsync<T>(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
            return default;

        string json = await File.ReadAllTextAsync(path, cancellationToken);
        return JsonConvert.DeserializeObject<T>(json);
    }

    public async Task WriteAsync<T>(string path, T value, CancellationToken cancellationToken = default)
    {
        string? directory = Path.GetDirectoryName(path);
        
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
        
        string tempPath = path + ".tmp";

        string json = JsonConvert.SerializeObject(value, JsonOptions);
        await File.WriteAllTextAsync(tempPath, json, cancellationToken);
        
        File.Move(tempPath, path, overwrite: true);
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }

    public bool Exists(string path)
    {
        return File.Exists(path);
    }
}