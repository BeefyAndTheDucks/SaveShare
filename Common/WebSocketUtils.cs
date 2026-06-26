using System.Net.WebSockets;
using System.Text;

namespace Common;

public static class WebSocketUtils
{
    public const int MESSAGE_BUFFER_SIZE = 1024;
    
    private static readonly SemaphoreSlim BufferSemaphore = new(1, 1);
    private static readonly byte[] Buffer = new byte[MESSAGE_BUFFER_SIZE];
    
    public static async Task<string> ReceiveString(WebSocket ws, CancellationToken ct = default)
    {
        using MemoryStream ms = new();
        
        WebSocketReceiveResult result;
        do
        {
            await BufferSemaphore.WaitAsync(ct);
            result = await ws.ReceiveAsync(Buffer, ct);

            if (result.MessageType == WebSocketMessageType.Close)
                break;
                
            ms.Write(Buffer, 0, result.Count);
            BufferSemaphore.Release();
        } while (!result.EndOfMessage);
            
        ms.Seek(0, SeekOrigin.Begin);

        if (result.MessageType == WebSocketMessageType.Text)
        {
            using StreamReader reader = new(ms, Encoding.UTF8);
            string message = await reader.ReadToEndAsync(ct);
            return message;
        }

        return "";
    }

    public static async Task SendString(WebSocket ws, string message, CancellationToken ct = default)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(message);

        for (int offset = 0; offset < bytes.Length; offset += MESSAGE_BUFFER_SIZE)
        {
            int count = Math.Min(MESSAGE_BUFFER_SIZE, bytes.Length - offset);
            bool endOfMessage = offset + count >= bytes.Length;

            await ws.SendAsync(
                new ArraySegment<byte>(bytes, offset, count),
                WebSocketMessageType.Text,
                endOfMessage,
                ct);
        }

        if (bytes.Length == 0)
        {
            await ws.SendAsync(
                ArraySegment<byte>.Empty,
                WebSocketMessageType.Text,
                true,
                ct);
        }
    }
}