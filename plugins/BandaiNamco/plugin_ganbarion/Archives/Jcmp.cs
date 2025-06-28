using Komponent.IO;
using Komponent.Streams;
using Kompression;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_ganbarion.Archives
{
    class Jcmp
    {
        private static readonly int HeaderSize = 0x14;

        private Jarc _jarc = new();
        private JcmpHeader _header;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = ReadHeader(br);

            // Decompress data
            var jarcStream = new MemoryStream();
            Compressions.ZLib.Build().Decompress(new SubStream(input, 0x14, _header.compSize), jarcStream);
            jarcStream.Position = 0;

            return _jarc.Load(jarcStream);
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var dataOffset = HeaderSize;

            // Save and compress jarc
            var jarcStream = new MemoryStream();
            _jarc.Save(jarcStream, files);
            jarcStream.Position = 0;

            output.Position = dataOffset;
            Compressions.ZLib.Build().Compress(jarcStream, output);

            // Write header
            _header.decompSize = (int)jarcStream.Length;
            _header.compSize = (int)(output.Length - HeaderSize);
            _header.fileSize = (int)output.Length;

            output.Position = 0;
            WriteHeader(_header, bw);
        }

        private JcmpHeader ReadHeader(BinaryReaderX reader)
        {
            return new JcmpHeader
            {
                magic = reader.ReadString(4),
                fileSize = reader.ReadInt32(),
                unk1 = reader.ReadInt32(),
                compSize = reader.ReadInt32(),
                decompSize = reader.ReadInt32()
            };
        }

        private void WriteHeader(JcmpHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.fileSize);
            writer.Write(header.unk1);
            writer.Write(header.compSize);
            writer.Write(header.decompSize);
        }
    }
}
