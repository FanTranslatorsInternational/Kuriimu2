using System;
using System.IO;
using System.Threading.Tasks;
using DiscUtils;
using DiscUtils.Iso9660;
using Kontract.Extensions;
using Kontract.Interfaces.Progress;
using Kontract.Interfaces.Providers;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_sony.Archives
{
    class Ps2DiscArchiveFileInfo : IArchiveFileInfo
    {
        private readonly DiscFileInfo _fileInfo;
        private Stream _baseStream;
        private Stream _setStream;

        /// <inheritdoc />
        public bool UsesCompression => false;

        /// <inheritdoc />
        public bool ContentChanged { get; set; }

        /// <inheritdoc />
        public Guid[] PluginIds { get; set; }

        /// <inheritdoc />
        public UPath FilePath
        {
            get => ((UPath)_fileInfo.FullName.Split(';')[0]).ToAbsolute();
            set => _fileInfo.MoveTo(value.FullName);
        }

        /// <inheritdoc />
        public long FileSize => _fileInfo.Length;

        public Ps2DiscArchiveFileInfo(DiscFileInfo fileInfo)
        {
            _fileInfo = fileInfo;
        }

        public Task<Stream> GetFileData(ITemporaryStreamProvider temporaryStreamProvider = null, IProgressContext progress = null)
        {
            return Task.FromResult(GetStream());
        }

        public void SetFileData(Stream fileData)
        {
            ContentChanged = true;
            _setStream = fileData;
        }

        public void SaveFileData(CDBuilder builder)
        {
            builder.AddFile(FilePath.ToRelative().FullName, GetStream());
        }

        private Stream GetStream()
        {
            if (_setStream != null)
                return _setStream;

            return _baseStream ??= _fileInfo.OpenRead();
        }
    }
}
