namespace Client.Interfaces;

public interface IAppDataPaths
{
    string AppDataDirectory { get; }
    string UserFilePath { get; }
    string LocalSavesFilePath { get; }
    string AppSettingsFilePath { get; }
}