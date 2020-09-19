using System.Collections.Generic;
using System.IO;
using System.Text;
using Kontract.Interfaces.Progress;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;

namespace plugin_level5.Wii.Archives
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

    static class BlnSubSupport
    {
        public static string GuessExtension(Stream input)
        {
            var magicSamples = CollectMagicSamples(input);

            if (magicSamples.Contains(0x55AA382D))
                return "arc";

            if (magicSamples.Contains(0x52415344))
                return "rasd";

            if (magicSamples.Contains(0x53485458))
                return "shtx";

            if (magicSamples.Contains(0x53534144))
                return "ssad";

            if (magicSamples.Contains(0x434d504b))
                return "cpmk";

            if (magicSamples.Contains(StringToUInt32("bres")))
                return "bres";

            return "bin";
        }

        private static uint StringToUInt32(string text)
        {
            return BufferToUInt32(Encoding.UTF8.GetBytes(text));
        }

        private static IList<uint> CollectMagicSamples(Stream input)
        {
            var bkPos = input.Position;

            // Get 3 samples to check magic with compression
            input.Position = bkPos;
            var magic1 = PeekUInt32(input);
            input.Position = bkPos + 1;
            var magic2 = PeekUInt32(input);
            input.Position = bkPos + 2;
            var magic3 = PeekUInt32(input);

            return new[] { magic1, magic2, magic3 };
        }

        private static uint PeekUInt32(Stream input)
        {
            var bkPos = input.Position;

            var buffer = new byte[4];
            input.Read(buffer, 0, 4);

            input.Position = bkPos;

            return BufferToUInt32(buffer);
        }

        private static uint BufferToUInt32(byte[] buffer)
        {
            return (uint)((buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3]);
        }
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
