using System.Text;
using Komponent.Contract.Enums;
using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_nintendo.Archives
{
    public class U8
    {
        private const int HeaderSize_ = 0x20;
        private const int EntrySize_ = 0xC;

        public List<IArchiveFile> Load(Stream input)
        {
            var typeReader = new BinaryTypeReader();
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            // Read header
            var header = typeReader.Read<U8Header>(br);

            // Parse file system
            var fileSystemParser = new DefaultU8FileSystem(UPath.Root);
            return fileSystemParser.Parse(input, header.entryDataOffset, header.entryDataSize, 0).ToList();
        }

        public void Save(Stream output, List<IArchiveFile> files)
        {
            var u8TreeBuilder = new U8TreeBuilder(Encoding.ASCII);
            u8TreeBuilder.Build(files.Select(x => ("/." + x.FilePath.FullName, x)).ToArray());

            var entries = u8TreeBuilder.Entries;
            var nameStream = u8TreeBuilder.NameStream;

            var namePosition = HeaderSize_ + entries.Count * EntrySize_;
            var dataOffset = (namePosition + (int)nameStream.Length + 0x1F) & ~0x1F;

            var typeWriter = new BinaryTypeWriter();
            using var bw = new BinaryWriterX(output, ByteOrder.BigEndian);

            // Write names
            bw.BaseStream.Position = namePosition;
            nameStream.Position = 0;
            nameStream.CopyTo(bw.BaseStream);
            bw.WriteAlignment(0x20);

            // Write files
            foreach (var (u8Entry, afi) in entries.Where(x => x.Item2 != null))
            {
                bw.WriteAlignment(0x20);
                var fileOffset = (int)bw.BaseStream.Position;

                var writtenSize = afi.WriteFileData(bw.BaseStream);

                u8Entry.offset = fileOffset;
                u8Entry.size = (int)writtenSize;
            }

            // Write entries
            bw.BaseStream.Position = HeaderSize_;
            typeWriter.WriteMany(entries.Select(x => x.Item1), bw);

            // Write header
            bw.BaseStream.Position = 0;
            typeWriter.Write(new U8Header
            {
                entryDataOffset = HeaderSize_,
                entryDataSize = entries.Count * EntrySize_ + (int)nameStream.Length,
                dataOffset = dataOffset
            }, bw);
            bw.WritePadding(0x10, 0xCC);
        }
    }
}
