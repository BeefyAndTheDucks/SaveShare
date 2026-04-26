using Common;
using Server;

namespace ServerTests;

public sealed class SaveRegistryTests : IDisposable
{
    private readonly string _testPath;
    
    public SaveRegistryTests()
    {
        // Give this specific test run a unique folder
        _testPath = Path.Combine(Path.GetTempPath(), "ServerTests_" + Guid.NewGuid());
        ServerSettings.SavePath = _testPath; 
        Directory.CreateDirectory(_testPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testPath))
            Directory.Delete(_testPath, true);
    }
    
    [Fact]
    public async Task TestCreateReadAndDeleteSave()
    {
        SaveId saveId = SaveRegistry.GenerateSaveId();
        SaveInfo baseSaveInfo = new()
        {
            SaveId = saveId,
            Name = "Test Save"
        };
        Result createResult = await SaveRegistry.CreateSave(baseSaveInfo, TestContext.Current.CancellationToken);
        Assert.True(createResult.Succeeded, createResult.Error);
        
        Result<SaveInfo> readResult = await SaveRegistry.GetSaveInfo(saveId, TestContext.Current.CancellationToken);
        Assert.True(readResult.Succeeded, readResult.Error);
        Assert.Equal(baseSaveInfo, readResult.Value);
        
        Result deleteResult = await SaveRegistry.DeleteSave(baseSaveInfo, TestContext.Current.CancellationToken);
        Assert.True(deleteResult.Succeeded, deleteResult.Error);
        Assert.False(File.Exists(SaveRegistry.GetSaveMetaPath(saveId)));
    }
    
    [Fact]
    public async Task TestUpdateSaveInfo()
    {
        SaveId saveId = SaveRegistry.GenerateSaveId();
        SaveInfo baseSaveInfo = new SaveInfo
        {
            SaveId = saveId,
            Name = "Test Save"
        };
        Result createResult = await SaveRegistry.CreateSave(baseSaveInfo, TestContext.Current.CancellationToken);
        Assert.True(createResult.Succeeded, createResult.Error);
        
        SaveInfo updatedSaveInfo = new SaveInfo
        {
            SaveId = saveId,
            Name = "Test Save 2"
        };
        
        Result updateResult = await SaveRegistry.UpdateSaveInfo(updatedSaveInfo, TestContext.Current.CancellationToken);
        Assert.True(updateResult.Succeeded, updateResult.Error);
        
        Result<SaveInfo> readResult = await SaveRegistry.GetSaveInfo(saveId, TestContext.Current.CancellationToken);
        Assert.True(readResult.Succeeded, readResult.Error);
        Assert.Equal(updatedSaveInfo, readResult.Value);
    }
    
    [Fact]
    public async Task TestUpdateSaveInfoWithFunc()
    {
        SaveId saveId = SaveRegistry.GenerateSaveId();
        SaveInfo baseSaveInfo = new SaveInfo
        {
            SaveId = saveId,
            Name = "Test Save"
        };
        Result createResult = await SaveRegistry.CreateSave(baseSaveInfo, TestContext.Current.CancellationToken);
        Assert.True(createResult.Succeeded, createResult.Error);
        
        Result updateResult = await SaveRegistry.UpdateSaveInfo(saveId, info =>
        {
            info.Name = "Test Save 2";
            return info;
        }, TestContext.Current.CancellationToken);
        Assert.True(updateResult.Succeeded, updateResult.Error);
        
        SaveInfo expectedSaveInfo = new SaveInfo
        {
            SaveId = saveId,
            Name = "Test Save 2"
        };
        Result<SaveInfo> readResult = await SaveRegistry.GetSaveInfo(saveId, TestContext.Current.CancellationToken);
        Assert.True(readResult.Succeeded, readResult.Error);
        Assert.Equal(expectedSaveInfo, readResult.Value);
    }
    
    [Fact]
    public async Task TestCheckoutAndRelease()
    {
        SaveId saveId = SaveRegistry.GenerateSaveId();
        SaveInfo baseSaveInfo = new SaveInfo
        {
            SaveId = saveId,
            Name = "Test Save"
        };
        Result createResult = await SaveRegistry.CreateSave(baseSaveInfo, TestContext.Current.CancellationToken);
        Assert.True(createResult.Succeeded, createResult.Error);
        
        Result releaseResult1 = await SaveRegistry.Release(saveId, "user1", TestContext.Current.CancellationToken);
        Assert.True(releaseResult1.Succeeded, releaseResult1.Error);
        
        Result checkoutResult1 = await SaveRegistry.TryCheckout(saveId, "user1", TestContext.Current.CancellationToken);
        Assert.True(checkoutResult1.Succeeded, checkoutResult1.Error);
        
        Result checkoutResult2 = await SaveRegistry.TryCheckout(saveId, "user2", TestContext.Current.CancellationToken);
        Assert.False(checkoutResult2.Succeeded, "The save should be checked out by user1, so user2 can't check it out.");
        
        Result releaseResult2 = await SaveRegistry.Release(saveId, "user2", TestContext.Current.CancellationToken);
        Assert.False(releaseResult2.Succeeded, "user2 is not the owner of the save, so they can't release it.");
        
        Result releaseResult3 = await SaveRegistry.Release(saveId, "user1", TestContext.Current.CancellationToken);
        Assert.True(releaseResult3.Succeeded, releaseResult3.Error);
    }
    
    [Fact]
    public async Task TestForceRelease()
    {
        SaveId saveId = SaveRegistry.GenerateSaveId();
        SaveInfo baseSaveInfo = new SaveInfo
        {
            SaveId = saveId,
            Name = "Test Save"
        };
        Result createResult = await SaveRegistry.CreateSave(baseSaveInfo, TestContext.Current.CancellationToken);
        Assert.True(createResult.Succeeded, createResult.Error);
        
        Result checkoutResult1 = await SaveRegistry.TryCheckout(saveId, "user1", TestContext.Current.CancellationToken);
        Assert.True(checkoutResult1.Succeeded, checkoutResult1.Error);
        
        Result checkoutResult2 = await SaveRegistry.TryCheckout(saveId, "user2", TestContext.Current.CancellationToken);
        Assert.False(checkoutResult2.Succeeded, "User 2 shouldn't be able to check it out because it's already checked out by user1.");
        
        Result forceReleaseResult = await SaveRegistry.ForceRelease(saveId, TestContext.Current.CancellationToken);
        Assert.True(forceReleaseResult.Succeeded, forceReleaseResult.Error);
        
        Result checkoutResult3 = await SaveRegistry.TryCheckout(saveId, "user2", TestContext.Current.CancellationToken);
        Assert.True(checkoutResult3.Succeeded, checkoutResult3.Error);
    }
    
    [Fact]
    public async Task TestGetSaveIds()
    {
        SaveId saveId1 = SaveRegistry.GenerateSaveId();
        SaveId saveId2 = SaveRegistry.GenerateSaveId();
        SaveInfo saveInfo1 = new SaveInfo
        {
            SaveId = saveId1,
            Name = "Test Save 1"
        };
        SaveInfo saveInfo2 = new SaveInfo
        {
            SaveId = saveId2,
            Name = "Test Save 1"
        };
        
        Result createResult1 = await SaveRegistry.CreateSave(saveInfo1, TestContext.Current.CancellationToken);
        Assert.True(createResult1.Succeeded, createResult1.Error);
        
        Result createResult2 = await SaveRegistry.CreateSave(saveInfo2, TestContext.Current.CancellationToken);
        Assert.True(createResult2.Succeeded, createResult2.Error);
        
        SaveId[] saves = SaveRegistry.GetSaveIds();
        Assert.Equal(2, saves.Length);
        Assert.Contains(saveId1, saves);
        Assert.Contains(saveId1, saves);
    }
    
    [Fact]
    public async Task TestGetSaves()
    {
        SaveId saveId1 = SaveRegistry.GenerateSaveId();
        SaveId saveId2 = SaveRegistry.GenerateSaveId();
        SaveInfo saveInfo1 = new SaveInfo
        {
            SaveId = saveId1,
            Name = "Test Save 1"
        };
        SaveInfo saveInfo2 = new SaveInfo
        {
            SaveId = saveId2,
            Name = "Test Save 1"
        };
        
        Result createResult1 = await SaveRegistry.CreateSave(saveInfo1, TestContext.Current.CancellationToken);
        Assert.True(createResult1.Succeeded, createResult1.Error);
        
        Result createResult2 = await SaveRegistry.CreateSave(saveInfo2, TestContext.Current.CancellationToken);
        Assert.True(createResult2.Succeeded, createResult2.Error);
        
        SaveInfo[] saves = await SaveRegistry.GetSaves(TestContext.Current.CancellationToken);
        Assert.Equal(2, saves.Length);
        Assert.Contains(saveInfo1, saves);
        Assert.Contains(saveInfo2, saves);
    }
    
    [Fact]
    public async Task TestConcurrentCheckoutRace()
    {
        SaveId saveId = SaveRegistry.GenerateSaveId();
        Result createResult = await SaveRegistry.CreateSave(new SaveInfo { SaveId = saveId, Name = "Race Test" }, TestContext.Current.CancellationToken);
        Assert.True(createResult.Succeeded, createResult.Error);
        
        const int taskCount = 20;
        List<Task<Result>> tasks = [];

        for (int i = 0; i < taskCount; i++)
        {
            string userId = $"user{i}";
            tasks.Add(Task.Run(() => SaveRegistry.TryCheckout(saveId, userId)));
        }

        Result[] results = await Task.WhenAll(tasks);

        // Assert: Exactly one task should have succeeded
        int successCount = results.Count(r => r.Succeeded);
        Assert.Equal(1, successCount);
    }
    
    [Fact]
    public async Task TestGetSaveInfoWithCorruptedFile()
    {
        SaveId saveId = SaveRegistry.GenerateSaveId();
        string path = SaveRegistry.GetSaveMetaPath(saveId);
    
        // Manually create a file with invalid JSON content
        await File.WriteAllTextAsync(path, "{ \"Invalid\": [Unclosed Bracket", TestContext.Current.CancellationToken);

        Result<SaveInfo> result = await SaveRegistry.GetSaveInfo(saveId, TestContext.Current.CancellationToken);
    
        Assert.False(result.Succeeded, "Should fail when trying to parse corrupted JSON.");
    }
    
    [Fact]
    public async Task TestGetSavesWithMixedCorruptedFiles()
    {
        SaveId validId = SaveRegistry.GenerateSaveId();
        SaveId corruptId = SaveRegistry.GenerateSaveId();

        // Create one valid save
        Result createResult = await SaveRegistry.CreateSave(new SaveInfo { SaveId = validId, Name = "Valid" }, TestContext.Current.CancellationToken);
        Assert.True(createResult.Succeeded, createResult.Error);
    
        // Manually create one corrupted file
        await File.WriteAllTextAsync(SaveRegistry.GetSaveMetaPath(corruptId), "Not JSON", TestContext.Current.CancellationToken);

        SaveInfo[] saves = await SaveRegistry.GetSaves(TestContext.Current.CancellationToken);

        // Assert: It should skip the corrupted one and return the valid one
        Assert.Single(saves);
        Assert.Equal(validId, saves[0].SaveId);
    }
    
    [Fact]
    public async Task TestUpdateNonExistentSave()
    {
        SaveId saveId = SaveRegistry.GenerateSaveId();
        SaveInfo info = new SaveInfo { SaveId = saveId, Name = "Non-existent" };

        Result result = await SaveRegistry.UpdateSaveInfo(info, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("doesn't already exist", result.Error);
    }

    [Fact]
    public async Task TestDeleteNonExistentSave()
    {
        SaveId saveId = SaveRegistry.GenerateSaveId();
    
        Result result = await SaveRegistry.DeleteSave(saveId, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal("Save metadata not found.", result.Error);
    }
    
    [Fact]
    public async Task TestConcurrentUpdateAndDelete()
    {
        SaveId saveId = SaveRegistry.GenerateSaveId();
        Result createResult = await SaveRegistry.CreateSave(new SaveInfo { SaveId = saveId, Name = "Race" }, TestContext.Current.CancellationToken);
        Assert.True(createResult.Succeeded, createResult.Error);

        // Try to update and delete at the same time
        Task<Result> updateTask = Task.Run(() => SaveRegistry.UpdateSaveInfo(saveId, i => i));
        Task<Result> deleteTask = Task.Run(() => SaveRegistry.DeleteSave(saveId));

        await Task.WhenAll(updateTask, deleteTask);

        // One must succeed, the other might fail depending on order, 
        // but the system should not throw a raw IOException.
        Assert.True(updateTask.IsCompletedSuccessfully);
        Assert.True(deleteTask.IsCompletedSuccessfully);
    }
    
    [Fact]
    public async Task TestGetSaveIdsIgnoresForeignFiles()
    {
        SaveId saveId = SaveRegistry.GenerateSaveId();
        Result createResult = await SaveRegistry.CreateSave(new SaveInfo { SaveId = saveId, Name = "Valid" }, TestContext.Current.CancellationToken);
        Assert.True(createResult.Succeeded, createResult.Error);

        // Add a file with a different extension
        string foreignFile = Path.Combine(SaveRegistry.GetSaveDirectory(), "notes.txt");
        await File.WriteAllTextAsync(foreignFile, "This is not a save info file.", TestContext.Current.CancellationToken);

        SaveId[] ids = SaveRegistry.GetSaveIds();

        Assert.Single(ids);
        Assert.Equal(saveId, ids[0]);
    }
    
    [Fact]
    public async Task TestReleaseByNonOwnerDoesNotUnlock()
    {
        SaveId saveId = SaveRegistry.GenerateSaveId();
        Result createResult = await SaveRegistry.CreateSave(new SaveInfo { SaveId = saveId, Name = "Lock Test" }, TestContext.Current.CancellationToken);
        Assert.True(createResult.Succeeded, createResult.Error);

        // User A checks it out
        Result checkoutResult = await SaveRegistry.TryCheckout(saveId, "UserA", TestContext.Current.CancellationToken);
        Assert.True(checkoutResult.Succeeded, checkoutResult.Error);

        // User B tries to release it
        Result releaseResult = await SaveRegistry.Release(saveId, "UserB", TestContext.Current.CancellationToken);
        Assert.False(releaseResult.Succeeded);

        // Verify UserA still owns it
        Result<SaveInfo> readResult = await SaveRegistry.GetSaveInfo(saveId, TestContext.Current.CancellationToken);
        Assert.Equal("UserA", readResult.Value.CheckedOutByUserName);
    }
    
    [Fact]
    public async Task TestUpdateCannotChangeSaveId()
    {
        SaveId originalId = SaveRegistry.GenerateSaveId();
        SaveId maliciousId = SaveRegistry.GenerateSaveId();
        Result createResult = await SaveRegistry.CreateSave(new SaveInfo { SaveId = originalId, Name = "Safe" }, TestContext.Current.CancellationToken);
        Assert.True(createResult.Succeeded, createResult.Error);

        Result result = await SaveRegistry.UpdateSaveInfo(originalId, info =>
        {
            info.SaveId = maliciousId; // Attempting to hijack the ID
            return info;
        }, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("may not change the SaveId", result.Error);
    }
    
    [Fact]
    public async Task TestGetSavesWithLargeDataset()
    {
        const int saveCount = 50;
        List<SaveId> generatedIds = [];

        for (int i = 0; i < saveCount; i++)
        {
            SaveId id = SaveRegistry.GenerateSaveId();
            generatedIds.Add(id);
            Result createResult = await SaveRegistry.CreateSave(new SaveInfo { SaveId = id, Name = $"Save {i}" }, TestContext.Current.CancellationToken);
            Assert.True(createResult.Succeeded, createResult.Error);
        }

        SaveInfo[] results = await SaveRegistry.GetSaves(TestContext.Current.CancellationToken);

        Assert.Equal(saveCount, results.Length);
        foreach (SaveId id in generatedIds)
        {
            Assert.Contains(results, s => s.SaveId == id);
        }
    }
    
    [Fact]
    public async Task TestGetRealSavePathValidation()
    {
        SaveId saveId = SaveRegistry.GenerateSaveId();
        SaveInfo info = new SaveInfo { SaveId = saveId };
        Result createResult = await SaveRegistry.CreateSave(info, TestContext.Current.CancellationToken);
        Assert.True(createResult.Succeeded, createResult.Error);

        // 1. Test failure when the directory is missing
        Result<string> pathResult = info.GetRealSavePath();
        Assert.False(pathResult.Succeeded);

        // 2. Test success when the directory exists
        string expectedDirectoryPath = Path.Combine(SaveRegistry.GetSaveDirectory(), saveId.ToString());
        Directory.CreateDirectory(expectedDirectoryPath);

        pathResult = info.GetRealSavePath();
        Assert.True(pathResult.Succeeded);
        Assert.Equal(expectedDirectoryPath, pathResult.Value);
    }
    
    [Fact]
    public async Task TestGetSavesReturnsEmptyArrayWhenDirectoryIsEmpty()
    {
        SaveInfo[] saves = await SaveRegistry.GetSaves(TestContext.Current.CancellationToken);
        
        Assert.NotNull(saves);
        Assert.Empty(saves);
    }

    [Fact]
    public async Task TestHasCheckout()
    {
        SaveId saveId = SaveRegistry.GenerateSaveId();
        SaveInfo info = new SaveInfo { SaveId = saveId };
        Result createResult = await SaveRegistry.CreateSave(info, TestContext.Current.CancellationToken);
        Assert.True(createResult.Succeeded, createResult.Error);
        
        Result<bool> hasCheckoutResult1 = await SaveRegistry.HasCheckout(saveId, "User1", TestContext.Current.CancellationToken);
        Assert.True(hasCheckoutResult1.Succeeded, hasCheckoutResult1.Error);
        Assert.False(hasCheckoutResult1.Value, "User1 should not have a checkout.");
        
        Result checkoutResult = await SaveRegistry.TryCheckout(saveId, "User1", TestContext.Current.CancellationToken);
        Assert.True(checkoutResult.Succeeded, checkoutResult.Error);
        
        Result<bool> hasCheckoutResult2 = await SaveRegistry.HasCheckout(saveId, "User1", TestContext.Current.CancellationToken);
        Assert.True(hasCheckoutResult2.Succeeded, hasCheckoutResult2.Error);
        Assert.True(hasCheckoutResult2.Value, "User1 should have a checkout.");
        
        Result<bool> hasCheckoutResult3 = await SaveRegistry.HasCheckout(saveId, "User2", TestContext.Current.CancellationToken);
        Assert.True(hasCheckoutResult3.Succeeded, hasCheckoutResult3.Error);
        Assert.False(hasCheckoutResult3.Value, "User2 should not have a checkout.");
        
        Result releaseResult = await SaveRegistry.Release(saveId, "User1", TestContext.Current.CancellationToken);
        Assert.True(releaseResult.Succeeded, releaseResult.Error);
        
        Result<bool> hasCheckoutResult4 = await SaveRegistry.HasCheckout(saveId, "User1", TestContext.Current.CancellationToken);
        Assert.True(hasCheckoutResult4.Succeeded, hasCheckoutResult4.Error);
        Assert.False(hasCheckoutResult4.Value, "User1 should not have a checkout after release.");
    }
}