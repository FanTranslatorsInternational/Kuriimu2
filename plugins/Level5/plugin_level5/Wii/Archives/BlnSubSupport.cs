using System.IO;
using Kontract.Interfaces.Progress;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;

namespace plugin_level5.Wii.Archives
{
    class BlnSubEntry
    {
        public int archiveIndex;    // index to an external bin
        public int archiveOffset;   // offset into that external bin
        public int size;
    }

    class BlnSubArchiveFileInfo : ArchiveFileInfo
    {
        private const int BlockSize_ = 0x4000;

        public BlnSubEntry Entry { get; }

        public long OriginalSize { get; }

        public BlnSubArchiveFileInfo(Stream fileData, string filePath, BlnSubEntry entry) :
            base(fileData, filePath)
        {
            Entry = entry;
            OriginalSize = fileData.Length;
        }

        public BlnSubArchiveFileInfo(Stream fileData, string filePath, BlnSubEntry entry, IKompressionConfiguration configuration, long decompressedSize) :
            base(fileData, filePath, configuration, decompressedSize)
        {
            Entry = entry;
            OriginalSize = fileData.Length;
        }

        public override long SaveFileData(Stream output, bool compress, IProgressContext progress = null)
        {
            var writtenSize = base.SaveFileData(output, compress, progress);

            // Pad to original size
            var paddedSize = OriginalSize - writtenSize;
            if (paddedSize > 0)
            {
                var padding = new byte[paddedSize];
                output.Write(padding, 0, padding.Length);

                writtenSize += paddedSize;
            }

            //// Pad to block
            //if (writtenSize % BlockSize_ != 0)
            //{
            //    var paddedSize = (int)(BlockSize_ - writtenSize % BlockSize_);
            //    output.Write(new byte[paddedSize], 0, paddedSize);

            //    writtenSize += paddedSize;
            //}

            // Return padded size as written
            return writtenSize;
        }
    }
}
