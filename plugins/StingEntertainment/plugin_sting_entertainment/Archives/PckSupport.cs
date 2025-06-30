using Komponent.IO;

namespace plugin_sting_entertainment.Archives
{
    class PckHeader
    {
        public string magic;
        public int size;
    }

    class PckEntry
    {
        public int offset;
        public int size;
    }

    static class PckSupport
    {
        public static PckHeader ReadHeader(BinaryReaderX reader)
        {
            return new PckHeader
            {
                magic = reader.ReadString(8),
                size = reader.ReadInt32()
            };
        }

        public static void WriteHeader(PckHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.size);
        }
    }
}
