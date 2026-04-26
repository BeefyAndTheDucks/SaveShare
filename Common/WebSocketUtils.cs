using System.Buffers;
using System.Net.WebSockets;
using System.Text;

namespace Common;

public static class WebSocketUtils
{
    public static async Task<string> ReceiveString(WebSocket ws, CancellationToken ct = default)
    {
        using MemoryStream ms = new();
        
        byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
        WebSocketReceiveResult result;
        do
        {
            result = await ws.ReceiveAsync(buffer, ct);

            if (result.MessageType == WebSocketMessageType.Close)
                break;
                
            ms.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);
        ArrayPool<byte>.Shared.Return(buffer);
            
        ms.Seek(0, SeekOrigin.Begin);

        if (result.MessageType == WebSocketMessageType.Text)
        {
            using StreamReader reader = new(ms, Encoding.UTF8);
            string message = await reader.ReadToEndAsync(ct);
            return message;
        }

        return "";
    }
}