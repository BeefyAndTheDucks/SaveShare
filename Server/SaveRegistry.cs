using Common;

namespace Server;

public static class SaveRegistry
{
    private static readonly SemaphoreSlim FileOperationSemaphore = new(1, 1);
    private static readonly SemaphoreSlim LockUnlockOperationSemaphore = new(1, 1);
    
    public static SaveId GenerateSaveId() => SaveId.NewSaveId();

    public static string GetSaveDirectory()
    {
        if (!Directory.Exists(ServerSettings.SavePath))
            Directory.CreateDirectory(ServerSettings.SavePath);
        return ServerSettings.SavePath;
    }
        
    public static string GetSaveMetaPath(SaveId saveId) => Path.Combine(GetSaveDirectory(), saveId + SaveInfo.FILE_EXTENSION);
    
    public static async Task<Result<SaveInfo>> GetSaveInfo(SaveId saveId, CancellationToken cancellationToken = default)
    {
        string metadataFilePath = GetSaveMetaPath(saveId);
        if (!File.Exists(metadataFilePath))
            return Result<SaveInfo>.Failure("Save metadata not found.");

        await FileOperationSemaphore.WaitAsync(cancellationToken);
        string json = await File.ReadAllTextAsync(metadataFilePath, cancellationToken);
        FileOperationSemaphore.Release();
        return SaveInfo.Deserialize(json);
    }

    public static async Task<Result> CreateSave(SaveInfo saveInfo, CancellationToken cancellationToken = default)
    {
        string metadataFilePath = GetSaveMetaPath(saveInfo.SaveId);
        if (File.Exists(metadataFilePath))
            return Result.Failure($"Save metadata already exists, to overwrite use {nameof(UpdateSaveInfo)}.");
        
        await FileOperationSemaphore.WaitAsync(cancellationToken);
        await File.WriteAllTextAsync(metadataFilePath, saveInfo.Serialize(), cancellationToken);
        FileOperationSemaphore.Release();
        return Result.Success();
    }

    public static async Task<Result<SaveId>> CreateSave(CancellationToken cancellationToken = default)
    {
        SaveId id = GenerateSaveId();
        Result createResult = await CreateSave(new SaveInfo { SaveId = id }, cancellationToken);
        if (!createResult.Succeeded)
            return Result<SaveId>.Failure(createResult.Error);
        return id;
    }

    public static async Task<Result> UpdateSaveInfo(SaveInfo saveInfo, CancellationToken cancellationToken = default)
    {
        string metadataFilePath = GetSaveMetaPath(saveInfo.SaveId);
        if (!File.Exists(metadataFilePath))
            return Result.Failure($"Save metadata doesn't already exist, to create a new one use {nameof(CreateSave)}.");

        await FileOperationSemaphore.WaitAsync(cancellationToken);
        await File.WriteAllTextAsync(metadataFilePath, saveInfo.Serialize(), cancellationToken);
        FileOperationSemaphore.Release();
        return Result.Success();
    }

    public static async Task<Result> UpdateSaveInfo(SaveId saveId, Func<SaveInfo, SaveInfo> updateMethod, CancellationToken cancellationToken = default)
    {
        Result<SaveInfo> saveInfoResult = await GetSaveInfo(saveId, cancellationToken);
        if (!saveInfoResult.Succeeded)
            return saveInfoResult.ToBasicResult();
        SaveInfo newSaveInfo = updateMethod(saveInfoResult.Value);
        if (newSaveInfo.SaveId != saveId)
            return Result.Failure($"You may not change the SaveId in {nameof(UpdateSaveInfo)}.");
        return await UpdateSaveInfo(newSaveInfo, cancellationToken);
    }

    public static async Task<Result> DeleteSave(SaveId saveId, CancellationToken cancellationToken = default)
    {
        string metadataFilePath = GetSaveMetaPath(saveId);
        if (!File.Exists(metadataFilePath))
            return Result.Failure("Save metadata not found.");

        await FileOperationSemaphore.WaitAsync(cancellationToken);
        File.Delete(metadataFilePath);
        FileOperationSemaphore.Release();
        return Result.Success();
    }

    public static async Task<Result> DeleteSave(SaveInfo save, CancellationToken cancellationToken = default) => await DeleteSave(save.SaveId, cancellationToken);

    public static string GetRealSavePathNoExistsCheck(SaveId saveId) => Path.Combine(GetSaveDirectory(), saveId.ToString());
    public static Result<string> GetRealSavePath(SaveId saveId)
    {
        string savePath = GetRealSavePathNoExistsCheck(saveId);
        if (Directory.Exists(savePath))
            return savePath;
        return Result<string>.Failure("Real save doesn't exist.");
    }

    public static Result<string> GetRealSavePath(SaveInfo saveInfo) => GetRealSavePath(saveInfo.SaveId);

    public static async Task<Result> TryCheckout(SaveId saveId, string userName, CancellationToken cancellationToken = default)
    {
        bool succeeded = false;
        await LockUnlockOperationSemaphore.WaitAsync(cancellationToken);
        Result updateResult = await UpdateSaveInfo(saveId, info =>
        {
            if (string.IsNullOrEmpty(info.CheckedOutByUserName) || info.CheckedOutByUserName == userName)
            {
                succeeded = true;
                info.CheckedOutByUserName = userName;
                info.CheckedOutAt = DateTime.Now;
            }

            return info;
        }, cancellationToken);
        LockUnlockOperationSemaphore.Release();
        
        if (!updateResult.Succeeded)
            return updateResult;
        
        return Result.FromFlag(succeeded, "Someone else has checked out this save.");
    }

    public static async Task<Result<bool>> HasCheckout(SaveId saveId, string userName,
        CancellationToken cancellationToken = default)
    {
        await LockUnlockOperationSemaphore.WaitAsync(cancellationToken);
        Result<SaveInfo> getSaveInfoResult = await GetSaveInfo(saveId, cancellationToken);
        LockUnlockOperationSemaphore.Release();
        
        if (!getSaveInfoResult.Succeeded)
            return Result<bool>.Failure(getSaveInfoResult.Error);
        return getSaveInfoResult.Value.CheckedOutByUserName == userName;
    }

    public static async Task<Result> Release(SaveId saveId, string userName, CancellationToken cancellationToken = default)
    {
        bool succeeded = false;
        await LockUnlockOperationSemaphore.WaitAsync(cancellationToken);
        Result updateResult = await UpdateSaveInfo(saveId, info =>
        {
            if (info.CheckedOutByUserName == userName || string.IsNullOrEmpty(info.CheckedOutByUserName))
            {
                succeeded = true;
                info.CheckedOutByUserName = "";
            }

            return info;
        }, cancellationToken);
        LockUnlockOperationSemaphore.Release();
        
        if (!updateResult.Succeeded)
            return updateResult;
        
        return Result.FromFlag(succeeded, "You haven't checked out this save.");
    }

    public static async Task<Result> ForceRelease(SaveId saveId, CancellationToken cancellationToken = default)
    {
        await LockUnlockOperationSemaphore.WaitAsync(cancellationToken);
        Result result = await UpdateSaveInfo(saveId, info =>
        {
            info.CheckedOutByUserName = "";

            return info;
        }, cancellationToken);
        LockUnlockOperationSemaphore.Release();
        return result;
    }
    
    public static SaveId[] GetSaveIds()
    {
        string[] saves = Directory.GetFiles(GetSaveDirectory(), "*" + SaveInfo.FILE_EXTENSION);
        return saves.Select(Path.GetFileNameWithoutExtension).Select(SaveId.Parse!).ToArray();
    }
    
    public static async Task<SaveInfo[]> GetSaves(CancellationToken cancellationToken = default)
    {
        SaveId[] saveIds = GetSaveIds();
        IEnumerable<Task<Result<SaveInfo>>> tasks = saveIds.Select(id => GetSaveInfo(id, cancellationToken));
        Result<SaveInfo>[] results = await Task.WhenAll(tasks);
        
        return results
            .Where(r => r.Check())
            .Select(r => r.Value)
            .ToArray();
    }
    
    public static bool SaveExists(SaveId saveId) => File.Exists(GetSaveMetaPath(saveId));
}