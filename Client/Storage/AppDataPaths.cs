using System;
using System.IO;
using Client.Interfaces;

namespace Client.Storage;

public sealed class AppDataPaths : IAppDataPaths
{
    public string AppDataDirectory { get; }
    public string UserFilePath => Path.Combine(AppDataDirectory, "user.json");
    public string LocalSavesFilePath => Path.Combine(AppDataDirectory, "saves.json");
    public string AppSettingsFilePath => Path.Combine(AppDataDirectory, "settings.json");

    public AppDataPaths()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        
        AppDataDirectory = Path.Combine(appData, "SaveShare");
        
        Directory.CreateDirectory(AppDataDirectory);
        
        Console.WriteLine($"Saving configuration to: {AppDataDirectory}");
    }
}