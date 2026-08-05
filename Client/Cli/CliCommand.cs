using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Client.Commands;

public class CliCommand : AsyncCommandBase
{
    private readonly CancellationTokenSource _cts = new();
    
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

            await CliHelpers.Setup(_cts.Token);
        
            REPL();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }
    }

    private void REPL()
    {
        while (true)
        {
            Console.WriteLine();
            Console.Write("> ");
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
                continue;
            if (input == "exit")
            {
                Console.WriteLine("Shutting down...");
                _cts.Cancel();
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
                    break;
                case "clear":
                    Console.Clear();
                    break;
                default:
                    Console.WriteLine("Unknown command.");
                    break;
            }
        }
    }

    protected override Command GetCommand()
    {
        return new Command("cli");
    }
}