namespace Konnect.Streams;

/// <summary>
/// A <see cref="Stream"/> to wrap a <see cref="FileStream"/> and deletes its corresponding file on closing.
/// </summary>
internal class TemporaryStream(FileStream baseStream) : Stream
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

    /// <summary>
    /// Closes the underlying <see cref="FileStream"/> and deletes the corresponding file.
    /// </summary>
    public override void Close()
    {
        base.Close();

        baseStream.Close();
        if (File.Exists(baseStream.Name))
            File.Delete(baseStream.Name);
    }
}