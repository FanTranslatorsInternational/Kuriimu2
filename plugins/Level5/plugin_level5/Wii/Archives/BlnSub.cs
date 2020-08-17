using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_level5.Wii.Archives
{
    // TODO: Test plugin
    // Game: Inazuma Eleven GO Strikers 2013
    // HINT: Despite being on Wii, this archive is Little Endian
    // HINT: Unbelievably ugly archive. Ignore everything that's done here and move on with your life, god dammit
    class BlnSub
    {
        public IList<ArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read files
            var result = new List<ArchiveFileInfo>();

            var index = 0;
            while (br.BaseStream.Position < input.Length)
            {
                var sample = br.ReadInt32();
                if (sample == 0x7FFF)
                    break;

                br.BaseStream.Position -= 4;
                var entry = br.ReadType<BlnSubEntry>();

                if (entry.size == 0)
                    break;

                var stream = new SubStream(input, br.BaseStream.Position, entry.size);
                result.Add(CreateAfi(stream, index++, entry));

                br.BaseStream.Position += entry.size;
            }

            return result;
        }

        public void Save(Stream output, IList<ArchiveFileInfo> files)
        {
            // Write files
            using var bw = new BinaryWriterX(output);
            foreach (var file in files.Cast<BlnSubArchiveFileInfo>())
            {
                var startOffset = output.Position;
                output.Position += 0xC;

                var writtenSize = file.SaveFileData(output);

                var endOffset = startOffset + writtenSize + 0xC;
                output.Position = startOffset;
                bw.WriteType(file.Entry);

                output.Position = endOffset;
            }

            // Write end entry
            bw.Write(0x7FFF);
            bw.WriteAlignment(0x1000);
        }

        private ArchiveFileInfo CreateAfi(Stream stream, int index, BlnSubEntry entry)
        {
            var compressionMagic = ReadInt32LittleEndian(stream);
            if (compressionMagic != 0xa755aafc)
                return new BlnSubArchiveFileInfo(stream, CreateFileName(index, stream, false), entry);

            stream.Position = 0;
            return new BlnSubArchiveFileInfo(stream, CreateFileName(index, stream, true), entry, Kompression.Implementations.Compressions.SpikeChunsoft, PeekDecompressedSize(stream));

        }

        private string CreateFileName(int index, Stream input, bool isCompressed)
        {
            input.Position = isCompressed ? 0xC : 0;
            var extension = BlnSubSupport.GuessExtension(input);

            return $"{index:00000000}.{extension}";
        }

        private long PeekDecompressedSize(Stream stream)
        {
            var bkPos = stream.Position;

            stream.Position = 4;
            var decompressedSize = ReadInt32LittleEndian(stream);

            stream.Position = bkPos;
            return decompressedSize;
        }

        private uint ReadInt32LittleEndian(Stream input)
        {
            var buffer = new byte[4];
            input.Read(buffer, 0, 4);

            return (uint)((buffer[3] << 24) | (buffer[2] << 16) | (buffer[1] << 8) | buffer[0]);
        }
    }
}
