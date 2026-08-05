using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Client.Interfaces;

namespace Client.Commands.Services;

public class CliModalService : IModalService
{
    public async Task<bool> ShowAsync(string title, string message, string yes = "Ok", string? no = null, CancellationToken cancellationToken = default)
    {
        int preferredMaxWidth = Console.WindowWidth;
        
        string modifiedTitle = $"┌─[{title}]";
        
        int maxWidth = Math.Max(modifiedTitle.Length + 2, preferredMaxWidth); // +2 for the padding chars

        int charIndex = 0;
        List<string> lines = [];
        
        StringBuilder line = new();
        StringBuilder word = new();
        
        int lineLength = 0;
        
        while (charIndex < message.Length)
        {
            while (charIndex < message.Length && message[charIndex] != ' ')
                word.Append(message[charIndex++]);
            charIndex++;
            
            if (lineLength + 1 + word.Length < maxWidth - 4)
            {
                if (lineLength > 0)
                    line.Append(' ');
                line.Append(word);
                lineLength += 1 + word.Length;
                word.Clear();
                continue;
            }

            lines.Add(line.ToString());
            line.Clear();
            line.Append(word);
            lineLength = word.Length;
            word.Clear();
        }
        
        if (line.Length > 0)
            lines.Add(line.ToString());

        int width = Math.Max(lines.MaxBy(l => l.Length)?.Length ?? maxWidth, modifiedTitle.Length + 2);
        
        StringBuilder modifiedDescriptionSb = new();
        foreach (string lineForDescription in lines)
        {
            modifiedDescriptionSb.Append("\n│ ");
            modifiedDescriptionSb.Append(lineForDescription.PadRight(width + 1, ' ') + '│');
        }
        string modifiedDescription = modifiedDescriptionSb.ToString();
        
        modifiedTitle = modifiedTitle.PadRight(width + 3, '─') + '┐';

        StringBuilder actionTextSb = new();
        actionTextSb.Append("└─[1]>["); actionTextSb.Append(yes); actionTextSb.Append(']');
        if (no is not null)
            actionTextSb.Append("|[2]>[").Append(no).Append(']');
        string actionText = actionTextSb.ToString().PadRight(width + 3, '─') + '┘';
        
        Console.WriteLine();
        Console.Write    (modifiedTitle);
        Console.WriteLine(modifiedDescription);
        Console.WriteLine(actionText);
        Console.Write("> ");
        while (true)
        {
            char inputtedChar = Console.ReadKey(true).KeyChar;
            switch (inputtedChar)
            {
                case '1':
                    Console.Write(inputtedChar);
                    return true;
                case '2' when no is not null:
                    Console.Write(inputtedChar);
                    return false;
                default:
                    continue;
            }
        }
    }
}