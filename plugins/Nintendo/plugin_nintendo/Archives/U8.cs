using System.Text;
using Komponent.Contract.Enums;
using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_nintendo.Archives
{
    public class U8
    {
        private const int HeaderSize_ = 0x20;
        private const int EntrySize_ = 0xC;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            // Read header
            var header = ReadHeader(br);

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
            WriteEntries(entries, bw);

            // Write header
            var header = new U8Header
            {
                tag = 0x55aa382d,
                entryDataOffset = HeaderSize_,
                entryDataSize = entries.Count * EntrySize_ + (int)nameStream.Length,
                dataOffset = dataOffset
            };

            bw.BaseStream.Position = 0;
            WriteHeader(header, bw);
            bw.WritePadding(0x10, 0xCC);
        }

        private U8Header ReadHeader(BinaryReaderX reader)
        {
            return new U8Header
            {
                tag = reader.ReadUInt32(),
                entryDataOffset = reader.ReadInt32(),
                entryDataSize = reader.ReadInt32(),
                dataOffset = reader.ReadInt32()
            };
        }

        private void WriteHeader(U8Header header, BinaryWriterX writer)
        {
            writer.Write(header.tag);
            writer.Write(header.entryDataOffset);
            writer.Write(header.entryDataSize);
            writer.Write(header.dataOffset);
        }

        private void WriteEntries(IList<(U8Entry, IArchiveFile)> entries, BinaryWriterX writer)
        {
            foreach ((U8Entry entry, IArchiveFile) entry in entries)
                WriteEntry(entry.entry, writer);
        }

        private void WriteEntry(U8Entry entry, BinaryWriterX writer)
        {
            writer.Write(entry.tmp1);
            writer.Write(entry.offset);
            writer.Write(entry.size);
        }
    }
}
