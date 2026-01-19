using System;
using System.IO;

namespace Swiftlet.Util
{
    /// <summary>
    /// A stream wrapper that reports progress as bytes are read.
    /// Useful for tracking upload progress with HttpClient's StreamContent.
    /// </summary>
    public class ProgressStream : Stream
    {
        private readonly Stream _inner;
        private readonly Action<long> _onProgress;
        private long _bytesRead;

        public ProgressStream(Stream inner, Action<long> onProgress)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _onProgress = onProgress;
            _bytesRead = 0;
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
            if (bytesRead > 0)
            {
                _bytesRead += bytesRead;
                _onProgress?.Invoke(_bytesRead);
            }
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
    }
}
