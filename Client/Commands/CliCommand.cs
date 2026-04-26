using System;
using System.CommandLine;

namespace Client.Commands;

public class CliCommand : CommandBase
{
    protected override void Invoke(ParseResult parseResult)
    {
        Console.WriteLine("CLI Mode");
    }

    protected override Command GetCommand()
    {
        return new Command("cli");
    }
}