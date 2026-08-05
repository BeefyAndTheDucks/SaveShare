using System;
using System.CommandLine;
using System.Threading.Tasks;
using Client.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Client.Commands.REPL;

public class RefreshCommand : REPLCommandBase
{
    public override string Command => "refresh";
    public override string Description => "Refreshes cloud saves.";
    
    private readonly Option<bool> _onlyLocal = new("-l", "--local", "-local", "--l") { Description = "Only refresh local saves." };
    private readonly Option<bool> _onlyRemote = new("-r", "--remote", "-remote", "--r") { Description = "Only refresh remote saves." };
    
    protected override async Task Invoke(ParseResult parseResult)
    {
        bool local = parseResult.GetValue(_onlyLocal);
        bool remote = parseResult.GetValue(_onlyRemote);
        
        ISaveCatalogService saveCatalogService = CliHelpers.Services.GetRequiredService<ISaveCatalogService>();

        Console.Write("Refreshing... ");
        
        switch (local)
        {
            case true when remote:
                await saveCatalogService.RefreshAsync();
                break;
            case true when !remote:
                await saveCatalogService.RefreshLocalSavesAsync();
                break;
            case false when remote:
                await saveCatalogService.RefreshCloudSavesAsync();
                break;
            case false when !remote:
                await saveCatalogService.RefreshAsync();
                break;
        }
        
        Console.WriteLine("Done!");
    }

    protected override Command GetCommand()
    {
        return new Command(Command, Description) { _onlyLocal, _onlyRemote };
    }
}