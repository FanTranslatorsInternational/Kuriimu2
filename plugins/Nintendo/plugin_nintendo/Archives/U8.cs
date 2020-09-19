using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_nintendo.Archives
{
    public class U8
    {
        private static int _headerSize = 0x20;
        private static int _entrySize = Tools.MeasureType(typeof(U8Entry));

        public IList<ArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            // Read header
            var header = br.ReadType<U8Header>();

            // Parse file system
            var fileSystemParser = new DefaultU8FileSystem(UPath.Root);
            return fileSystemParser.Parse(input, header.entryDataOffset, header.entryDataSize, 0).ToArray();
        }

        public void Save(Stream output, IList<ArchiveFileInfo> files)
        {
            var darcTreeBuilder = new U8TreeBuilder(Encoding.ASCII);
            darcTreeBuilder.Build(files.Select(x => ("/." + x.FilePath.FullName, x)).ToArray());

            var entries = darcTreeBuilder.Entries;
            var nameStream = darcTreeBuilder.NameStream;

            var namePosition = _headerSize + entries.Count * _entrySize;
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

                var writtenSize = afi.SaveFileData(bw.BaseStream);

                u8Entry.offset = fileOffset;
                u8Entry.size = (int)writtenSize;
            }

            // Write entries
            bw.BaseStream.Position = _headerSize;
            bw.WriteMultiple(entries.Select(x => x.Item1));

            // Write header
            bw.BaseStream.Position = 0;
            bw.WriteType(new U8Header
            {
                entryDataOffset = _headerSize,
                entryDataSize = entries.Count * _entrySize + (int)nameStream.Length,
                dataOffset = dataOffset
            });
            bw.WritePadding(0x10, 0xCC);
        }
    }
}
