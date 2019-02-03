using System.IO;
using Kontract.Interfaces.Archive;

namespace plugin_criware.CPK
{
    /// <summary>
    /// Stores information about each file in a <see cref="CPK"/> archive.
    /// </summary>
    public class CpkFileInfo : ArchiveFileInfo
    {
        private bool _fileDataChanged;

        private MemoryStream _decompressedFile;

        #region Properties

        /// <summary>
        /// Determines whether the file is and should be obfuscated.
        /// </summary>
        public bool Obfuscated { get; private set; }

        /// <summary>
        /// Stores the length of the file.
        /// </summary>
        public long FileLength { get; private set; }

        /// <summary>
        /// Stores the compressed length of the file.
        /// </summary>
        public long CompressedLength { get; private set; }

        // Overrides
        /// <summary>
        /// Returns the length of <see cref="FileData"/>.
        /// </summary>
        public override long? FileSize => FileLength;

        /// <summary>
        /// Provides a readable stream of the <see cref="FileData"/>.
        /// </summary>
        public override Stream FileData
        {
            get
            {
                if (FileLength != CompressedLength && !_fileDataChanged)
                    return _decompressedFile ?? (_decompressedFile = new MemoryStream(CRILAYLA.CRILAYLA.Decompress(_fileData)));

                return _fileData;
            }
            set
            {
                _fileData = value;
                FileLength = value.Length;
                _fileDataChanged = true;
            }
        }

        #endregion

        /// <summary>
        /// Instantiates a new instance of <see cref="CpkFileInfo"/>.
        /// </summary>
        /// <param name="obfuscated"></param>
        /// <param name="fileLength"></param>
        /// <param name="compressedLength"></param>
        public CpkFileInfo(Stream fileData, bool obfuscated, long fileLength, long compressedLength)
        {
            _fileData = fileData;
            Obfuscated = obfuscated;
            FileLength = fileLength;
            CompressedLength = compressedLength;
        }
    }
}
