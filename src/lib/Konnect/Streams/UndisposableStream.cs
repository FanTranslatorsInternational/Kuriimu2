namespace Konnect.Streams;

/// <summary>
/// A <see cref="Stream"/> to stub the disposing and closing methods.
/// </summary>
class UndisposableStream(Stream baseStream) : Stream
{
    /// <inheritdoc />
    public override bool CanRead => baseStream.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => baseStream.CanSeek;

    /// <inheritdoc />
    public override bool CanWrite => baseStream.CanWrite;

    /// <inheritdoc />
    public override long Length => baseStream.Length;

    /// <inheritdoc />
    public override long Position
    {
        get => baseStream.Position;
        set => baseStream.Position = value;
    }

    /// <inheritdoc />
    public override void Flush()
        => baseStream.Flush();

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
        => baseStream.Seek(offset, origin);

    /// <inheritdoc />
    public override void SetLength(long value)
        => baseStream.SetLength(value);

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
        => baseStream.Read(buffer, offset, count);

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
        => baseStream.Write(buffer, offset, count);
}