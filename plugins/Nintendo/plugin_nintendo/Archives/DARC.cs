using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_nintendo.Archives
{
    // HINT: DARC's can contain paths with dots. UPath will resolve to the current directory, and will therefore invalidate them
    // To act against this (desired) behaviour, the Afi will hold the original path, which will be used in the Save process to regenerate the tree

    public class Darc
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(DarcHeader));
        private static readonly int EntrySize = Tools.MeasureType(typeof(DarcEntry));

        private ByteOrder _byteOrder;

        #region Load

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            // Determine byte order
            input.Position += 4;
            br.ByteOrder = _byteOrder = br.ReadType<ByteOrder>();

            // Read header
            br.BaseStream.Position = 0;
            var header = br.ReadType<DarcHeader>();

            // Read entries
            br.BaseStream.Position = header.tableOffset;
            var rootEntry = br.ReadType<DarcEntry>();

            br.BaseStream.Position = header.tableOffset;
            var entries = br.ReadMultiple<DarcEntry>(rootEntry.size);

            // Read names
            var nameStream = new SubStream(input, br.BaseStream.Position, header.dataOffset - br.BaseStream.Position);

            // Add files
            using var nameBr = new BinaryReaderX(nameStream);

            var result = new List<IArchiveFileInfo>();
            var lastDirectoryEntry = entries[0];
            foreach (var entry in entries.Skip(1))
            {
                // A file does not know of its parent directory
                // The tree is structured so that the last directory entry read must hold the current file

                // Remember the last directory entry
                if (entry.IsDirectory)
                {
                    lastDirectoryEntry = entry;
                    continue;
                }

                // Find whole path recursively from lastDirectoryEntry
                var currentDirectoryEntry = lastDirectoryEntry;
                var currentPath = string.Empty;
                while (currentDirectoryEntry != entries[0])
                {
                    nameBr.BaseStream.Position = currentDirectoryEntry.NameOffset;
                    currentPath = Path.Combine(nameBr.ReadCStringUTF16(), currentPath);

                    currentDirectoryEntry = entries[currentDirectoryEntry.offset];
                }

                // Get file name
                nameBr.BaseStream.Position = entry.NameOffset;
                var fileName = Path.Combine(currentPath, nameBr.ReadCStringUTF16());

                var fileStream = new SubStream(input, entry.offset, entry.size);
                result.Add(new DarcArchiveFileInfo(fileStream, fileName));
            }

            return result;
        }

        #endregion

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            var darcTreeBuilder = new DarcTreeBuilder(Encoding.Unicode);
            darcTreeBuilder.Build(files.Cast<DarcArchiveFileInfo>().ToArray());

            var entries = darcTreeBuilder.Entries;
            var nameStream = darcTreeBuilder.NameStream;

            var namePosition = HeaderSize + entries.Count * EntrySize;

            using var bw = new BinaryWriterX(output, true, _byteOrder);

            // Write names
            bw.BaseStream.Position = namePosition;
            nameStream.Position = 0;
            nameStream.CopyTo(bw.BaseStream);
            bw.WriteAlignment(4);

            // Write files
            foreach (var (darcEntry, afi) in entries.Where(x => x.Item2 != null))
            {
                var alignment = 4;
                if (afi.FilePath.GetExtensionWithDot() == ".bclim" || afi.FilePath.GetExtensionWithDot() == ".arc" || afi.FilePath.GetExtensionWithDot() == ".snd")
                    alignment = 0x80;

                bw.WriteAlignment(alignment);
                var fileOffset = (int)bw.BaseStream.Position;

                var writtenSize = (afi as ArchiveFileInfo).SaveFileData(bw.BaseStream);

                darcEntry.offset = fileOffset;
                darcEntry.size = (int)writtenSize;
            }

            // Write entries
            bw.BaseStream.Position = HeaderSize;
            bw.WriteMultiple(entries.Select(x => x.Item1));

            // Write header
            bw.BaseStream.Position = 0;
            bw.WriteType(new DarcHeader
            {
                byteOrder = _byteOrder,
                dataOffset = entries.Where(x => x.Item2 != null).Select(x => x.Item1.offset).Min(),
                fileSize = (int)bw.BaseStream.Length,
                tableLength = entries.Count * EntrySize + (int)nameStream.Length
            });
        }
    }
}
