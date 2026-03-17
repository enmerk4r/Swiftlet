using System.IO;

namespace Swiftlet.Gh.Rhino8;

internal sealed class ProgressReadStream : Stream
{
    private readonly Stream _inner;
    private readonly Action<long> _onProgress;
    private long _bytesRead;

    public ProgressReadStream(Stream inner, Action<long> onProgress)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _onProgress = onProgress ?? throw new ArgumentNullException(nameof(onProgress));
    }

    public override bool CanRead => _inner.CanRead;
    public override bool CanSeek => _inner.CanSeek;
    public override bool CanWrite => _inner.CanWrite;
    public override long Length => _inner.Length;

    public override long Position
    {
        get => _inner.Position;
        set => _inner.Position = value;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int bytesRead = _inner.Read(buffer, offset, count);
        Report(bytesRead);
        return bytesRead;
    }

    public override int Read(Span<byte> buffer)
    {
        int bytesRead = _inner.Read(buffer);
        Report(bytesRead);
        return bytesRead;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int bytesRead = await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        Report(bytesRead);
        return bytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        int bytesRead = await _inner.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
        Report(bytesRead);
        return bytesRead;
    }

    public override void Flush() => _inner.Flush();

    public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

    public override void SetLength(long value) => _inner.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
        }

        base.Dispose(disposing);
    }

    private void Report(int bytesRead)
    {
        if (bytesRead <= 0)
        {
            return;
        }

        _bytesRead += bytesRead;
        _onProgress(_bytesRead);
    }
}
