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
    public class ArchiveFileInfo
    {
        private UPath _filePath;

        private IKompressionConfiguration _configuration;
        private Lazy<Stream> _decompressedStream;
        private long _decompressedSize;
        private bool _hasSetFileData;

        /// <summary>
        /// Determines if the FileData is compressed, and has to be handled as such
        /// </summary>
        private bool UsesCompression => _configuration != null;

        /// <summary>
        /// Determines if the content of this file info was modified.
        /// </summary>
        public bool ContentChanged { get; set; }

        /// <summary>
        /// Retrieve a list of plugins, this file can be opened with.
        /// Any other plugins won't be allowed to pass this file on.
        /// </summary>
        public Guid[] PluginIds { get; set; }

        /// <summary>
        /// The data stream for this file info.
        /// </summary>
        protected Stream FileData { get; set; }

        /// <summary>
        /// The path of the file info into the archive.
        /// </summary>
        public UPath FilePath
        {
            get => _filePath;
            set => _filePath = value.ToAbsolute();
        }

        /// <summary>
        /// The size of the file data.
        /// </summary>
        public virtual long FileSize => UsesCompression ? _decompressedSize : FileData?.Length ?? 0;

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
        }

        public ArchiveFileInfo(Stream fileData, string filePath,
            IKompressionConfiguration configuration, long decompressedSize) :
            this(fileData, filePath)
        {
            ContractAssertions.IsNotNull(configuration, nameof(configuration));

            _configuration = configuration;
            _decompressedSize = decompressedSize;
            _decompressedStream = new Lazy<Stream>(() => GetDecompressedStream(fileData, configuration));
        }

        /// <summary>
        /// Gets the file data from this file info.
        /// </summary>
        /// <param name="temporaryStreamProvider">A provider for temporary streams.</param>
        /// <param name="progress">The context to report progress to.</param>
        /// <returns>The file data for this file info.</returns>
        /// <remarks>The <see cref="ITemporaryStreamProvider"/> is used for decrypting or decompressing files temporarily onto the disk to minimize memory usage.</remarks>
        public virtual Task<Stream> GetFileData(ITemporaryStreamProvider temporaryStreamProvider = null, IProgressContext progress = null)
        {
            if (UsesCompression)
                return Task.Factory.StartNew(() => _decompressedStream.Value);

            return Task.FromResult(GetBaseStream());
        }

        /// <summary>
        /// Sets the file data for this file info.
        /// </summary>
        /// <param name="fileData">The new file data for this file info.</param>
        /// <remarks>This method should only set the file data, without compressing or encrypting the data yet.</remarks>
        public virtual void SetFileData(Stream fileData)
        {
            if (FileData == fileData)
                return;

            FileData.Close();
            FileData = fileData;

            _hasSetFileData = true;
            ContentChanged = true;

            if (UsesCompression)
            {
                _decompressedSize = fileData.Length;
                _decompressedStream = new Lazy<Stream>(GetBaseStream);
            }
        }

        /// <summary>
        /// Save the file data to an output stream.
        /// </summary>
        /// <param name="output">The output to write the file data to.</param>
        /// <param name="progress">The context to report progress to.</param>
        /// <returns>The size of the file written.</returns>
        public virtual long SaveFileData(Stream output, IProgressContext progress = null)
        {
            Stream dataToCopy;
            if (UsesCompression)
            {
                dataToCopy = _hasSetFileData ?
                    GetCompressedStream(FileData, _configuration) :
                    GetBaseStream();
            }
            else
            {
                dataToCopy = GetBaseStream();
            }

            progress?.ReportProgress($"Writing file '{FilePath}'.", 0, 1);

            // TODO: Change that to a manual bulk copy to better watch progress?
            dataToCopy.CopyTo(output);

            progress?.ReportProgress($"Writing file '{FilePath}'.", 1, 1);

            ContentChanged = false;

            return dataToCopy.Length;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return FilePath.FullName;
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
        /// Decompresses the given stream.
        /// </summary>
        /// <param name="fileData">The stream to decompress.</param>
        /// <param name="configuration">The compression configuration to use.</param>
        /// <returns>The decompressed stream.</returns>
        protected static Stream GetDecompressedStream(Stream fileData, IKompressionConfiguration configuration)
        {
            var ms = new MemoryStream();

            ms.Position = 0;
            fileData.Position = 0;

            configuration.Build().Decompress(fileData, ms);

            fileData.Position = 0;
            ms.Position = 0;

            return ms;
        }

        /// <summary>
        /// Compresses the given stream.
        /// </summary>
        /// <param name="fileData">The stream to compress.</param>
        /// <param name="configuration">The compression configuration to use.</param>
        /// <returns>The compressed stream.</returns>
        protected static Stream GetCompressedStream(Stream fileData, IKompressionConfiguration configuration)
        {
            var ms = new MemoryStream();

            ms.Position = 0;
            fileData.Position = 0;

            configuration.Build().Compress(fileData, ms);

            fileData.Position = 0;
            ms.Position = 0;

            return ms;
        }
    }
}
