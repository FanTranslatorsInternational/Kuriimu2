namespace Konnect.Streams;

/// <summary>
/// A <see cref="Stream"/> to stub the disposing and closing methods.
/// </summary>
class UndisposableStream : System.IO.Stream
{
    private readonly System.IO.Stream _baseStream;

    /// <inheritdoc />
    public override bool CanRead => _baseStream.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => _baseStream.CanSeek;

    /// <inheritdoc />
    public override bool CanWrite => _baseStream.CanWrite;

    /// <inheritdoc />
    public override long Length => _baseStream.Length;

    /// <inheritdoc />
    public override long Position
    {
        get => _baseStream.Position;
        set => _baseStream.Position = value;
    }

    /// <summary>
    /// Creates a new instance of <see cref="UndisposableStream"/>.
    /// </summary>
    /// <param name="baseStream">The stream to embed.</param>
    public UndisposableStream(System.IO.Stream baseStream)
    {
        _baseStream = baseStream;
    }

    /// <inheritdoc />
    public override void Flush()
        => _baseStream.Flush();

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
        => _baseStream.Seek(offset, origin);

    /// <inheritdoc />
    public override void SetLength(long value)
        => _baseStream.SetLength(value);

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
        => _baseStream.Read(buffer, offset, count);

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
        => _baseStream.Write(buffer, offset, count);
}