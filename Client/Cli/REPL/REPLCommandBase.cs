using System.Threading.Tasks;

namespace Client.Commands.REPL;

public abstract class REPLCommandBase
{
    public abstract string Command { get; }
    public abstract string Description { get; }
    public abstract Task Execute(string[] args);
}