using Kompression.Contract;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Management.Streams;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Contract.Progress;
using Konnect.Extensions;

namespace Konnect.Plugin.File.Archive;

/// <summary>
/// The base model to represent a loaded file in an archive state.
/// </summary>
public class ArchiveFile : IArchiveFile
{
    private readonly ArchiveFileInfo _fileInfo;

    private Lazy<Stream> _decompressedStream;
    private Lazy<Stream> _compressedStream;
    private Func<long> _getFileSizeAction;

    /// <inheritdoc />
    public Guid[]? PluginIds => _fileInfo.PluginIds;

    /// <inheritdoc />
    public bool UsesCompression => _fileInfo is CompressedArchiveFileInfo;

    /// <inheritdoc />
    public UPath FilePath
    {
        get => _fileInfo.FilePath.ToAbsolute();
        set
        {
            _fileInfo.FilePath = value.ToAbsolute();
            _fileInfo.ContentChanged = true;
        }
    }

    /// <inheritdoc />
    public virtual long FileSize => _getFileSizeAction();

    /// <inheritdoc />
    public bool IsFileDataInvalid => _fileInfo.FileData is { CanRead: false, CanWrite: false };

    /// <inheritdoc />
    public bool ContentChanged => _fileInfo.ContentChanged;

    /// <summary>
    /// Creates a new instance of <see cref="ArchiveFile"/>.
    /// </summary>
    /// <param name="fileInfo">The info for this file.</param>
    public ArchiveFile(ArchiveFileInfo fileInfo)
    {
        _fileInfo = fileInfo;

        if (fileInfo is CompressedArchiveFileInfo compressedInfo)
        {
            _getFileSizeAction = () => compressedInfo.DecompressedSize;
            _decompressedStream = new Lazy<Stream>(() => DecompressStream(compressedInfo.FileData, compressedInfo.Compression));
            _compressedStream = new Lazy<Stream>(GetBaseStream);
        }
        else
        {
            _getFileSizeAction = GetFileDataLength;
        }
    }

    /// <inheritdoc />
    public virtual Task<Stream> GetFileData(ITemporaryStreamManager? temporaryStreamManager = null, IProgressContext? progress = null)
    {
        return UsesCompression ? Task.Run(GetDecompressedStream) : Task.FromResult(GetBaseStream());
    }

    /// <inheritdoc />
    public virtual void SetFileData(Stream fileData)
    {
        if (_fileInfo.FileData == fileData)
            return;

        _fileInfo.FileData.Close();
        _fileInfo.FileData = fileData;

        _getFileSizeAction = GetFileDataLength;

        _fileInfo.ContentChanged = true;

        if (_fileInfo is not CompressedArchiveFileInfo compressedInfo)
            return;

        _decompressedStream = new Lazy<Stream>(GetBaseStream);
        _compressedStream = new Lazy<Stream>(() => CompressStream(fileData, compressedInfo.Compression));
    }

    /// <summary>
    /// Save the file data to an output stream.
    /// </summary>
    /// <param name="output">The output to write the file data to.</param>
    /// <param name="compress">If the file should be compressed, if compression is set.</param>
    /// <param name="progress">The context to report progress to.</param>
    /// <returns>The size of the file written.</returns>
    public long WriteFileData(Stream output, bool compress, IProgressContext? progress = null)
    {
        var dataToCopy = GetFinalStream(compress);

        progress?.ReportProgress($"Writing file '{FilePath}'.", 0, 1);

        // TODO: Change that to a manual bulk copy to better watch progress?
        dataToCopy.CopyTo(output);

        progress?.ReportProgress($"Writing file '{FilePath}'.", 1, 1);

        _fileInfo.ContentChanged = false;

        return dataToCopy.Length;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _fileInfo.FileData?.Dispose();
        _decompressedStream = null;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return FilePath.FullName;
    }

    #region Stream methods

    /// <summary>
    /// Get the final stream of FileData.
    ///     1. This is the compressed stream if a compression is set,
    ///     2. The decompressed stream if a compression is set but compression was disabled for this operation, or
    ///     3. The original FileData stream.
    /// </summary>
    /// <returns>The final stream.</returns>
    protected Stream GetFinalStream(bool compress = true)
    {
        if (!UsesCompression)
            return GetBaseStream();

        // If ArchiveFileInfo uses compression but file data should not be saved as compressed,
        //   get decompressed data (decompress it for the first time here, if necessary)
        // Otherwise use already compressed data
        return compress ? GetCompressedStream() : GetDecompressedStream();
    }

    /// <summary>
    /// Gets the base stream of data, without processing it further.
    /// </summary>
    /// <returns>The base stream of this instance.</returns>
    protected Stream GetBaseStream()
    {
        _fileInfo.FileData.Position = 0;
        return _fileInfo.FileData;
    }

    /// <summary>
    /// Gets the decompressed stream from the <see cref="Lazy{T}"/> instance.
    /// </summary>
    /// <returns>The decompressed stream of this instance.</returns>
    protected Stream GetDecompressedStream()
    {
        var decompressedStream = _decompressedStream.Value;

        decompressedStream.Position = 0;
        return decompressedStream;
    }

    /// <summary>
    /// Gets the compressed stream from the <see cref="Lazy{T}"/> instance.
    /// </summary>
    /// <returns>The compressed stream of this instance.</returns>
    protected Stream GetCompressedStream()
    {
        var compressedStream = _compressedStream.Value;

        compressedStream.Position = 0;
        return compressedStream;
    }

    #endregion

    #region De-/Compression

    /// <summary>
    /// Compresses the given stream.
    /// </summary>
    /// <param name="fileData">The stream to compress.</param>
    /// <param name="configuration">The compression configuration to use.</param>
    /// <returns>The compressed stream.</returns>
    protected static Stream CompressStream(Stream fileData, ICompression configuration)
    {
        var ms = new MemoryStream();

        ms.Position = 0;
        fileData.Position = 0;

        configuration.Compress(fileData, ms);

        fileData.Position = 0;
        ms.Position = 0;

        return ms;
    }

    /// <summary>
    /// Decompresses the given stream.
    /// </summary>
    /// <param name="fileData">The stream to decompress.</param>
    /// <param name="compression">The compression configuration to use.</param>
    /// <returns>The decompressed stream.</returns>
    protected static Stream DecompressStream(Stream fileData, ICompression compression)
    {
        var ms = new MemoryStream();

        ms.Position = 0;
        fileData.Position = 0;

        compression.Decompress(fileData, ms);

        fileData.Position = 0;
        ms.Position = 0;

        return ms;
    }

    #endregion

    #region Size delegates

    private long GetFileDataLength()
    {
        return _fileInfo.FileData?.Length ?? 0;
    }

    #endregion
}