namespace Common;

public sealed class NonClosingStreamShield(Stream inner) : Stream
{
    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => inner.CanSeek;
    public override bool CanWrite => inner.CanWrite;
    public override long Length => inner.Length;
    public override long Position { get => inner.Position; set => inner.Position = value; }

    public override void Flush() => inner.Flush();
    public override Task FlushAsync(CancellationToken ct) => inner.FlushAsync(ct);
    public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
    public override Task<int> ReadAsync(byte[] b, int o, int c, CancellationToken ct) => inner.ReadAsync(b, o, c, ct);
    public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
    public override Task WriteAsync(byte[] b, int o, int c, CancellationToken ct) => inner.WriteAsync(b, o, c, ct);
    public override void SetLength(long value) => inner.SetLength(value);
    public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

    protected override void Dispose(bool disposing) 
    {
        // Do nothing. We do not dispose the inner stream here.
    }

    public override async ValueTask DisposeAsync()
    {
        // Do nothing.
        await Task.CompletedTask;
    }
}