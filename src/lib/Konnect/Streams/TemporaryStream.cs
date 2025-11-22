namespace Konnect.Streams;

/// <summary>
/// A <see cref="Stream"/> to wrap a <see cref="FileStream"/> and deletes its corresponding file on closing.
/// </summary>
class TemporaryStream : System.IO.Stream
{
    private readonly FileStream _baseStream;

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
    /// Creates a new instance of <see cref="TemporaryStream"/>.
    /// </summary>
    /// <param name="baseStream">The <see cref="FileStream"/> to wrap.</param>
    public TemporaryStream(FileStream baseStream)
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

    /// <summary>
    /// Closes the underlying <see cref="FileStream"/> and deletes the corresponding file.
    /// </summary>
    public override void Close()
    {
        base.Close();

        _baseStream.Close();
        if (File.Exists(_baseStream.Name))
            File.Delete(_baseStream.Name);
    }
}