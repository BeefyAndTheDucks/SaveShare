using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Client.Commands.REPL;

namespace Client.Commands;

public class CliCommand : AsyncCommandBase
{
    private readonly CancellationTokenSource _cts = new();

    private readonly REPLCommandBase[] _commands =
    [
        new ListCommand()
    ];
    
    protected override async Task Invoke(ParseResult parseResult)
    {
        try
        {
            Console.WriteLine( "_____________________________");
            Console.WriteLine( "|  ____                     |");
            Console.WriteLine(@"| /SAVE\     WELCOME TO:    |");
            Console.WriteLine(@"| \SYNC/   Save Share CLI!  |");
            Console.WriteLine( "|  ‾‾‾‾                     |");
            Console.WriteLine( "| You've entered REPL mode. |");
            Console.WriteLine( "| This reuses the log-in    |");
            Console.WriteLine( "| session.                  |");
            Console.WriteLine( "| Press Ctrl+C to exit.     |");
            Console.WriteLine( "| Type 'help' for commands. |");
            Console.WriteLine( "‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾");
            Console.WriteLine();

            Console.CancelKeyPress += (_, _) => Console.Write("^C");
            Console.CancelKeyPress += (_, _) => _cts.Cancel();
            Console.CancelKeyPress += (_, _) => Console.WriteLine("\nGoodbye.");

            if (!await CliHelpers.Setup(_cts.Token))
                return;
        
            await REPL();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }
    }

    private async Task REPL()
    {
        while (true)
        {
            Console.WriteLine();
            Console.Write("> ");
            string? input = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(input))
                continue;
            if (input == "exit")
            {
                Console.WriteLine("Shutting down...");
                await _cts.CancelAsync();
                Console.WriteLine("Goodbye.");
                break;
            }

            switch (input)
            {
                case "help":
                    Console.WriteLine("Commands:");
                    Console.WriteLine("- help: Shows this message.");
                    Console.WriteLine("- exit: Exit the REPL.");
                    Console.WriteLine("- clear: Clears the screen.");
                    foreach (REPLCommandBase command in _commands)
                        LogHelpCommand(command.CreateCommand());
                    break;
                case "clear":
                    Console.Clear();
                    break;
                default:
                    if (!await TryEvaluateReplCommand(input))
                        Console.WriteLine("Unknown command.");
                    break;
            }
        }
    }

    private static void LogHelpCommand(Command command, int depth = 0)
    {
        string indent = new(' ', depth * 2);
        Console.WriteLine($"{indent}- {command.Name}: {command.Description}");
        foreach (Option option in command.Options)
            if (!option.Hidden)
                Console.WriteLine($"{indent}  - {option.Name}: {option.Description} ({string.Join(", ", option.Aliases)})");
        
        foreach (Argument argument in command.Arguments)
            if (!argument.Hidden)
                Console.WriteLine($"{indent}  - {argument.Name}: {argument.Description}");
        
        foreach (Command subcommand in command.Subcommands)
            if (!subcommand.Hidden)
                LogHelpCommand(subcommand, depth + 1);
    }

    private async Task<bool> TryEvaluateReplCommand(string input)
    {
        string[] args = input.Split(' ');
        string command = args[0];
        foreach (REPLCommandBase replCommand in _commands)
            if (command == replCommand.Command)
            {
                await replCommand.Execute(args[1..]);
                return true;
            }

        return false;
    }

    protected override Command GetCommand()
    {
        return new Command("cli");
    }
}