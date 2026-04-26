using System.Text;
using Common;
using Newtonsoft.Json;

namespace Server;

public static class UserRegistry
{
    private static readonly SemaphoreSlim FileOperationSemaphore = new(1, 1);

    private const string k_UserRegistryFilepath = "users.json";

    private static async Task<string> GetUserRegistryFile(CancellationToken cancellationToken = default)
    {
        if (File.Exists(k_UserRegistryFilepath)) return k_UserRegistryFilepath;
        
        await File.Create(k_UserRegistryFilepath).DisposeAsync();
        
        return k_UserRegistryFilepath;
    }
    
    public static async Task<Result<User>> CreateUser(string userName, CancellationToken cancellationToken = default)
    {
        await FileOperationSemaphore.WaitAsync(cancellationToken);
        
        string userRegistryFile = await GetUserRegistryFile(cancellationToken);
        string userRegistryJson = await File.ReadAllTextAsync(userRegistryFile, cancellationToken);
        User[] userRegistry = JsonConvert.DeserializeObject<User[]>(userRegistryJson) ?? [];

        if (userRegistry.Any(u => u.Username == userName))
        {
            FileOperationSemaphore.Release();
            return Result<User>.Failure("A user with that name already exists.");
        }
        
        User newUser = new(Guid.NewGuid(), userName);
        userRegistry = userRegistry.Append(newUser).ToArray();
        await File.WriteAllTextAsync(userRegistryFile, JsonConvert.SerializeObject(userRegistry), cancellationToken);
        
        FileOperationSemaphore.Release();
        
        return newUser;
    }
    
    public static async Task<Result<User>> GetUser(Guid userId, CancellationToken cancellationToken = default)
    {
        await FileOperationSemaphore.WaitAsync(cancellationToken);
        
        string userRegistryFile = await GetUserRegistryFile(cancellationToken);
        string userRegistryJson = await File.ReadAllTextAsync(userRegistryFile, cancellationToken);
        User[]? userRegistry = JsonConvert.DeserializeObject<User[]>(userRegistryJson);
        if (userRegistry is null)
        {
            FileOperationSemaphore.Release();
            return Result<User>.Failure("User not found");
        }

        User? user = userRegistry.FirstOrDefault(u => u.Id == userId);
        
        if (user is null)
        {
            FileOperationSemaphore.Release();
            return Result<User>.Failure("User not found");
        }
        
        FileOperationSemaphore.Release();

        return user;
    }
}
