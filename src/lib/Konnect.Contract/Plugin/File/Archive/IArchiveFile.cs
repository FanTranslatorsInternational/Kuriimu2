using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.Management.Streams;
using Konnect.Contract.Progress;

namespace Konnect.Contract.Plugin.File.Archive;

public interface IArchiveFile : IDisposable
{
    /// <summary>
    /// The predefined plugin ids to try to open the file with.
    /// </summary>
    public Guid[]? PluginIds { get; }

    /// <summary>
    /// Determines if the FileData is compressed, and has to be handled as such
    /// </summary>
    bool UsesCompression { get; }

    /// <summary>
    /// The path of the file info into the archive.
    /// </summary>
    UPath FilePath { get; set; }

    /// <summary>
    /// The size of the file data.
    /// </summary>
    long FileSize { get; }

    /// <summary>
    /// Determines if the file is invalid or closed and not eligible for use.
    /// </summary>
    bool IsFileDataInvalid { get; }

    /// <summary>
    /// Determines if the archive file was changed.
    /// </summary>
    bool ContentChanged { get; }

    /// <summary>
    /// Gets the (decompressed) file data from this file info.
    /// </summary>
    /// <param name="temporaryStreamProvider">A provider for temporary streams.</param>
    /// <param name="progress">The context to report progress to.</param>
    /// <returns>The file data for this file info.</returns>
    /// <remarks>The <see cref="ITemporaryStreamManager"/> is used for decrypting or decompressing files temporarily onto the disk to minimize memory usage.</remarks>
    Task<Stream> GetFileData(ITemporaryStreamManager? temporaryStreamProvider = null, IProgressContext? progress = null);

    /// <summary>
    /// Sets the file data for this file info.
    /// </summary>
    /// <param name="fileData">The new file data for this file info.</param>
    /// <remarks>This method should only set the file data, without compressing or encrypting the data yet.</remarks>
    void SetFileData(Stream fileData);

    /// <summary>
    /// Writes the file data to an output stream.
    /// </summary>
    /// <param name="output">The stream to write to.</param>
    /// <param name="compress">If the file should be compressed, if a compression is set.</param>
    /// <param name="progress">The context to report progress to.</param>
    /// <returns>The size of the written data.</returns>
    long WriteFileData(Stream output, bool compress = true, IProgressContext? progress = null);
}