using System;
using System.CommandLine;
using System.Threading.Tasks;
using Client.Interfaces;
using Client.Services;
using Common;
using Microsoft.Extensions.DependencyInjection;

namespace Client.Commands.REPL;

public class ListCommand : REPLCommandBase
{
    public override string Command => "list";
    public override string Description => "Lists all saves.";

    private readonly Option<bool> _alsoOutputRemoteSaves = new("-r", "--remote", "-remote", "--r")
    {
        Description = "Also output remote saves."
    };
    
    protected override async Task Invoke(ParseResult parseResult)
    {
        bool remote = parseResult.GetValue(_alsoOutputRemoteSaves);

        ISaveCatalogService saveCatalogService = CliHelpers.Services.GetRequiredService<ISaveCatalogService>();
        
        Console.WriteLine("Local saves:");
        
        var localSaves = saveCatalogService.LocalSaves;
        foreach (LocalSaveInfo localSave in localSaves)
            Console.WriteLine($"- \"{localSave.Name}\" at {localSave.LocalPath} (id: {localSave.SaveId})");

        if (!remote)
            return;

        Console.WriteLine();
        Console.WriteLine("Cloud saves:");
        
        var cloudSaves = saveCatalogService.CloudSaves;
        foreach (SaveInfo cloudSave in cloudSaves)
        {
            Console.Write($"- \"{cloudSave.Name}\" (type: {cloudSave.SaveType}) (id: {cloudSave.SaveId})");
            if (cloudSave.SaveType is SaveType.File)
                Console.Write($" (file extension: \"{cloudSave.FileExtension.TrimStart('.')})\"");
            if (!string.IsNullOrEmpty(cloudSave.CheckedOutByUserName))
                Console.Write($" (checked out by \"{cloudSave.CheckedOutByUserName}\" at {cloudSave.CheckedOutAt})");
            Console.Write($" (last changed by \"{cloudSave.LastSyncedByUserName}\" at {cloudSave.LastSyncedAt})");
            Console.WriteLine();
        }
    }

    protected override Command GetCommand()
    {
        return new Command(Command, Description) { _alsoOutputRemoteSaves };
    }
}