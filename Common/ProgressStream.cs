namespace Common;

public class ProgressStream(Stream inner, Action<long> onProgress) : Stream
{
    private long _bytesProcessed;
    
    public override void Flush() => inner.Flush();

    public override int Read(byte[] buffer, int offset, int count)
    {
        int n = inner.Read(buffer, offset, count);
        _bytesProcessed += n;
        onProgress(_bytesProcessed);
        return n;
    }
    
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int n = await inner.ReadAsync(buffer, cancellationToken);
        _bytesProcessed += n;
        onProgress(_bytesProcessed);
        return n;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        int n = await inner.ReadAsync(buffer, offset, count, cancellationToken);
        _bytesProcessed += n;
        onProgress(_bytesProcessed);
        return n;
    }

    public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

    public override void SetLength(long value) => inner.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count)
    {
        inner.Write(buffer, offset, count);
        _bytesProcessed += count;
        onProgress(_bytesProcessed);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await inner.WriteAsync(buffer, offset, count, cancellationToken);
        _bytesProcessed += count;
        onProgress(_bytesProcessed);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
    {
        await inner.WriteAsync(buffer, cancellationToken);
        _bytesProcessed += buffer.Length;
        onProgress(_bytesProcessed);
    }

    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => inner.CanSeek;
    public override bool CanWrite => inner.CanWrite;
    public override long Length => inner.Length;
    public override long Position { get => inner.Position; set => inner.Position = value; }
}