using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Kontract.Interfaces.Progress;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;
#pragma warning disable 649

namespace plugin_shade.Archives
{
	// Archive index maps to the following bins for Inazuma Eleven Strikers 2013
	// 0x00 => strap.bin
	// 0x01 => scn.bin
	// 0x02 => scn_sh.bin
	// 0x03 => ui.bin
	// 0x04 => dat.bin
	// 0x05 => grp.bin?
	
    class BlnSubEntry
    {
        public int archiveIndex;    // index to an external bin
        public int archiveOffset;   // offset into that external bin
        public int size;
    }


    class BlnSubArchiveFileInfo : ArchiveFileInfo
    {
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

            if (writtenSize > OriginalSize)
                throw new InvalidOperationException("The replaced file cannot be larger than its original.");

            // Pad to original size
            var paddedSize = OriginalSize - writtenSize;
            if (paddedSize > 0)
            {
                var padding = new byte[paddedSize];
                output.Write(padding, 0, padding.Length);

                writtenSize += paddedSize;
            }

            // Return padded size as written
            return writtenSize;
        }
    }
}
