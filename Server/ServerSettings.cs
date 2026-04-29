using Newtonsoft.Json;

namespace Server;

public class ServerSettings
{
    private const string k_SettingsFile = "settings.json";

    public static void Initialize()
    {
        _ = Instance;
    }
    
    public static ServerSettings Instance
    {
        get
        {
            if (field != null) return field;
            if (File.Exists(k_SettingsFile))
                return field = JsonConvert.DeserializeObject<ServerSettings>(File.ReadAllText(k_SettingsFile)) ?? PrintErrorAndCreateNew();
            
            Console.WriteLine($"No settings file found, creating new at {Path.GetFullPath(k_SettingsFile)}");
            File.WriteAllText(k_SettingsFile, JsonConvert.SerializeObject(field = new ServerSettings()));
            return field;

            ServerSettings PrintErrorAndCreateNew()
            {
                Console.Error.WriteLine("Failed to load configuration file, loading defaults.");
                return new ServerSettings();
            }
        }
    }
    
    public string SavePath { get; set; } = "Saves/";
}