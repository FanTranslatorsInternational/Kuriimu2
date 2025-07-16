using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_dotemu.Archives
{
    class Sor4Entry
    {
        public string path;
        public uint offset;
        public int flags;
        public int compSize;
    }

    class Sor4ArchiveFile : ArchiveFile
    {
        public Sor4Entry Entry { get; }

        public Sor4ArchiveFile(ArchiveFileInfo fileInfo, Sor4Entry entry) : base(fileInfo)
        {
            Entry = entry;
        }
    }

    class Sor4Support
    {
        public static Platform DeterminePlatform(Stream texListStream)
        {
            using var br = new BinaryReaderX(texListStream, true);

            var entry1 = ReadEntry(br);
            var entry2 = ReadEntry(br);

            texListStream.Position = 0;

            // Platform is determined by the alignment between the first 2 entries
            // Switch aligns all files to 16 bytes and precedes them with the decompressed size
            // Pc does not align files. It also does not precede them with the decompressed size
            if (entry1.compSize == entry2.offset)
                return Platform.Pc;

            return Platform.Switch;
        }

        public static Sor4Entry ReadEntry(BinaryReaderX reader)
        {
            return new Sor4Entry
            {
                path = reader.ReadString(),
                offset = reader.ReadUInt32(),
                flags = reader.ReadInt32(),
                compSize = reader.ReadInt32()
            };
        }

        public static void WriteEntry(Sor4Entry entry, BinaryWriterX writer)
        {
            writer.Write(entry.path);
            writer.Write(entry.offset);
            writer.Write(entry.flags);
            writer.Write(entry.compSize);
        }
    }

    enum Platform
    {
        Switch,
        Pc
    }
}
