using System;
using System.IO;
using Komponent.IO;
using Kontract.Interfaces.Archive;

namespace plugin_criware.CPK
{
    /// <summary>
    /// Stores information about each file in a <see cref="CPK"/> archive.
    /// </summary>
    public class CpkFileInfo : ArchiveFileInfo, IDisposable
    {
        private bool _fileDataChanged;

        private MemoryStream _decompressedFile;

        #region Properties

        /// <summary>
        /// The row associated with this file.
        /// </summary>
        public CpkRow Row { get; }

        /// <summary>
        /// Determines whether the file is and should be obfuscated.
        /// </summary>
        public bool Obfuscated { get; }

        /// <summary>
        /// Stores the length of the file.
        /// </summary>
        public long FileLength { get; private set; }

        /// <summary>
        /// Stores the compressed length of the file.
        /// </summary>
        public long CompressedLength { get; private set; }

        /// <summary>
        /// Gets whether the file is compressed.
        /// </summary>
        public bool Compressed { get; }

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
                if (Compressed && !_fileDataChanged)
                    //if (Obfuscated)
                    //{
                    //    var bytes = UtfTools.XorUtf(new BinaryReaderX(_fileData, true).ReadAllBytes());
                    //    var 
                    //    return _decompressedFile ?? (_decompressedFile = new MemoryStream(CRILAYLA.CRILAYLA.Decompress(_fileData)));
                    //}
                    //else
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
        public CpkFileInfo(Stream fileData, CpkRow row, bool obfuscated, long fileLength, long compressedLength)
        {
            _fileData = fileData;
            Row = row;
            Obfuscated = obfuscated;
            FileLength = fileLength;
            CompressedLength = compressedLength;
            Compressed = FileLength != CompressedLength;
        }

        /// <summary>
        /// Save the current file out
        /// </summary>
        /// <param name="output"></param>
        public void SaveFile(Stream output)
        {
            // TODO: Implement support for obfuscated files.

            // Update the file length when the data has changed.
            if (_fileDataChanged)
                FileLength = _fileData.Length;

            // Compress the new file if the original was compressed.
            if (Compressed && _fileDataChanged)
                _fileData = new MemoryStream(CRILAYLA.CRILAYLA.Compress(_fileData));

            // Update the compressed length and write out the file.
            CompressedLength = _fileData.Length;
            _fileData.CopyTo(output);
        }

        /// <inheritdoc />
        /// <summary>
        /// Clear out the decompressed file.
        /// </summary>
        public void Dispose()
        {
            _decompressedFile?.Dispose();
        }
    }
}
