using System;
using System.Threading;
using System.Threading.Tasks;
using Client.Interfaces;

namespace Client.Commands.Services;

public class CliModalService : IModalService
{
    public async Task<bool> ShowAsync(string title, string message, string yes = "Ok", string? no = null, CancellationToken cancellationToken = default)
    {
        while (true)
        {
            Console.WriteLine(title);
            Console.WriteLine(message);
            Console.Write($"[1] [{yes}]");
            if (no is not null) Console.Write($" [2] [{no}]");
            Console.WriteLine();
            Console.Write("> ");
            string input = Console.ReadLine()?.Trim() ?? throw new OperationCanceledException();
            switch (input)
            {
                case "1":
                    return true;
                case "2":
                    return false;
                default:
                    continue;
            }
        }
    }
}