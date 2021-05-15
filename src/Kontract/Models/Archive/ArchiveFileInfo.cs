using System;
using System.IO;
using System.Threading.Tasks;
using Kontract.Extensions;
using Kontract.Interfaces.Progress;
using Kontract.Interfaces.Providers;
using Kontract.Kompression.Configuration;
using Kontract.Models.IO;

namespace Kontract.Models.Archive
{
    /// <summary>
    /// The base model to represent a loaded file in an archive state.
    /// </summary>
    public class ArchiveFileInfo : IArchiveFileInfo
    {
        private readonly IKompressionConfiguration _configuration;

        private UPath _filePath;

        private Lazy<Stream> _decompressedStream;
        private Lazy<Stream> _compressedStream;
        private Func<long> _getFileSizeAction;

        /// <inheritdoc />
        public bool UsesCompression => _configuration != null;

        /// <inheritdoc />
        public bool ContentChanged { get; set; }

        /// <inheritdoc />
        public Guid[] PluginIds { get; set; }

        /// <summary>
        /// The data stream for this file info.
        /// </summary>
        protected Stream FileData { get; set; }

        /// <inheritdoc />
        public UPath FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value.ToAbsolute();
                ContentChanged = true;
            }
        }

        /// <inheritdoc />
        public virtual long FileSize => _getFileSizeAction();

        /// <inheritdoc />
        public bool IsFileDataInvalid => !FileData.CanRead && !FileData.CanWrite;

        /// <summary>
        /// Creates a new instance of <see cref="ArchiveFileInfo"/>.
        /// </summary>
        /// <param name="fileData">The data stream for this file info.</param>
        /// <param name="filePath">The path of the file into the archive.</param>
        public ArchiveFileInfo(Stream fileData, string filePath)
        {
            ContractAssertions.IsNotNull(fileData, nameof(fileData));
            ContractAssertions.IsNotNull(filePath, nameof(filePath));

            FileData = fileData;
            FilePath = filePath;

            ContentChanged = false;

            _getFileSizeAction = GetFileDataLength;
        }

        /// <summary>
        /// Creates a new instance of <see cref="ArchiveFileInfo"/>.
        /// </summary>
        /// <param name="fileData">The data stream for this file info.</param>
        /// <param name="filePath">The path of the file into the archive.</param>
        /// <param name="configuration">The <see cref="IKompressionConfiguration"/> for the compression algorithm to apply to the file.</param>
        /// <param name="decompressedSize">The decompressed size of the file.</param>
        public ArchiveFileInfo(Stream fileData, string filePath,
            IKompressionConfiguration configuration, long decompressedSize) :
            this(fileData, filePath)
        {
            ContractAssertions.IsNotNull(configuration, nameof(configuration));

            _configuration = configuration;

            _getFileSizeAction = () => decompressedSize;
            _decompressedStream = new Lazy<Stream>(() => DecompressStream(fileData, configuration));
            _compressedStream = new Lazy<Stream>(GetBaseStream);
        }

        /// <inheritdoc />
        public virtual Task<Stream> GetFileData(ITemporaryStreamProvider temporaryStreamProvider = null,
            IProgressContext progress = null)
        {
            return UsesCompression ? Task.Run(GetDecompressedStream) : Task.FromResult(GetBaseStream());
        }

        /// <inheritdoc />
        public virtual void SetFileData(Stream fileData)
        {
            if (FileData == fileData)
                return;

            FileData.Close();
            FileData = fileData;

            _getFileSizeAction = GetFileDataLength;

            ContentChanged = true;

            if (!UsesCompression)
                return;

            _decompressedStream = new Lazy<Stream>(GetBaseStream);
            _compressedStream = new Lazy<Stream>(() => CompressStream(fileData, _configuration));
        }

        /// <summary>
        /// Save the file data to an output stream.
        /// </summary>
        /// <param name="output">The output to write the file data to.</param>
        /// <param name="progress">The context to report progress to.</param>
        /// <returns>The size of the file written.</returns>
        public long SaveFileData(Stream output, IProgressContext progress = null)
        {
            return SaveFileData(output, true, progress);
        }

        /// <summary>
        /// Save the file data to an output stream.
        /// </summary>
        /// <param name="output">The output to write the file data to.</param>
        /// <param name="compress">If <see cref="UsesCompression"/> is true, determines if the file data may be compressed.</param>
        /// <param name="progress">The context to report progress to.</param>
        /// <returns>The size of the file written.</returns>
        public virtual long SaveFileData(Stream output, bool compress, IProgressContext progress = null)
        {
            var dataToCopy = GetFinalStream(compress);

            progress?.ReportProgress($"Writing file '{FilePath}'.", 0, 1);

            // TODO: Change that to a manual bulk copy to better watch progress?
            dataToCopy.CopyTo(output);

            progress?.ReportProgress($"Writing file '{FilePath}'.", 1, 1);

            ContentChanged = false;

            return dataToCopy.Length;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            FileData?.Dispose();
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
            FileData.Position = 0;
            return FileData;
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
        protected static Stream CompressStream(Stream fileData, IKompressionConfiguration configuration)
        {
            var ms = new MemoryStream();

            ms.Position = 0;
            fileData.Position = 0;

            configuration.Build().Compress(fileData, ms);

            fileData.Position = 0;
            ms.Position = 0;

            return ms;
        }

        /// <summary>
        /// Decompresses the given stream.
        /// </summary>
        /// <param name="fileData">The stream to decompress.</param>
        /// <param name="configuration">The compression configuration to use.</param>
        /// <returns>The decompressed stream.</returns>
        protected static Stream DecompressStream(Stream fileData, IKompressionConfiguration configuration)
        {
            var ms = new MemoryStream();

            ms.Position = 0;
            fileData.Position = 0;

            configuration.Build().Decompress(fileData, ms);

            fileData.Position = 0;
            ms.Position = 0;

            return ms;
        }

        #endregion

        #region Size delegates

        private long GetFileDataLength()
        {
            return FileData?.Length ?? 0;
        }

        #endregion
    }
}
