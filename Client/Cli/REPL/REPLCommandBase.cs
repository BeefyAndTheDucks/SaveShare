using System.CommandLine;
using System.Threading.Tasks;

namespace Client.Commands.REPL;

public abstract class REPLCommandBase : AsyncCommandBase
{
    public abstract string Command { get; }
    public abstract string Description { get; }

    public async Task Execute(string[] args)
    {
        Command root = GetCommand();
        root.SetAction(Invoke);
        ParseResult parseResult = root.Parse(args);
        await parseResult.InvokeAsync();
    }
}