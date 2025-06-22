using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_nintendo.Archives
{
    public class PC
    {
        private const int HeaderSize_ = 0x4;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = ReadHeader(br);

            // Read offsets
            var offsets = ReadUnsignedIntegers(br, header.entryCount);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < offsets.Length; i++)
            {
                var endOffset = i + 1 < offsets.Length ? offsets[i + 1] : input.Length;
                var fileStream = new SubStream(input, offsets[i], endOffset - offsets[i]);

                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = $"{i:00000000}.bin",
                    FileData = fileStream
                }));
            }

            return result;
        }

        public void Save(Stream output, List<IArchiveFile> files)
        {
            var dataPosition = (HeaderSize_ + (files.Count + 1) * 4 + 0x7F) & ~0x7F;

            using var bw = new BinaryWriterX(output);

            // Write files
            bw.BaseStream.Position = dataPosition;

            var offsets = new List<uint>();
            foreach (var file in files)
            {
                offsets.Add((uint)bw.BaseStream.Position);

                file.WriteFileData(bw.BaseStream);
                bw.WriteAlignment(0x80);
            }

            // Write offsets
            bw.BaseStream.Position = HeaderSize_;
            WriteUnsignedIntegers(offsets, bw);
            bw.Write(bw.BaseStream.Length);

            // Write header
            var header = new PcHeader
            {
                magic = "PC",
                entryCount = (short)files.Count
            };

            bw.BaseStream.Position = 0;
            WriteHeader(header, bw);
        }

        private PcHeader ReadHeader(BinaryReaderX reader)
        {
            return new PcHeader
            {
                magic = reader.ReadString(2),
                entryCount = reader.ReadInt16()
            };
        }

        private uint[] ReadUnsignedIntegers(BinaryReaderX reader, int count)
        {
            var result = new uint[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadUInt32();

            return result;
        }

        private void WriteHeader(PcHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator:false);
            writer.Write(header.entryCount);
        }

        private void WriteUnsignedIntegers(IList<uint> entries, BinaryWriterX writer)
        {
            foreach (uint entry in entries)
                writer.Write(entry);
        }
    }
}
