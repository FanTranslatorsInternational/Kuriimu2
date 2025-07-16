using System.Text;
using Komponent.Contract.Enums;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;

namespace plugin_nintendo.Archives
{
    // HINT: DARC's can contain paths with dots. UPath will resolve to the current directory, and will therefore invalidate them
    // To act against this (desired) behaviour, the Afi will hold the original path, which will be used in the Save process to regenerate the tree

    public class Darc
    {
        private const int HeaderSize_ = 0x1C;
        private const int EntrySize_ = 0xC;

        private ByteOrder _byteOrder;

        #region Load

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true, ByteOrder.BigEndian);

            // Determine byte order
            input.Position += 4;
            br.ByteOrder = _byteOrder = (ByteOrder)br.ReadUInt16();

            // Read header
            br.BaseStream.Position = 0;
            var header = ReadHeader(br);

            // Read entries
            br.BaseStream.Position = header.tableOffset;
            var rootEntry = ReadEntry(br);

            br.BaseStream.Position = header.tableOffset;
            var entries = ReadEntries(br, rootEntry.size);

            // Read names
            var nameStream = new SubStream(input, br.BaseStream.Position, header.dataOffset - br.BaseStream.Position);

            // Add files
            using var nameBr = new BinaryReaderX(nameStream, Encoding.Unicode);

            var result = new List<IArchiveFile>();
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
                    currentPath = Path.Combine(nameBr.ReadNullTerminatedString(), currentPath);

                    currentDirectoryEntry = entries[currentDirectoryEntry.offset];
                }

                // Get file name
                nameBr.BaseStream.Position = entry.NameOffset;
                var fileName = Path.Combine(currentPath, nameBr.ReadNullTerminatedString());

                var fileStream = new SubStream(input, entry.offset, entry.size);
                result.Add(new DarcArchiveFile(new ArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = fileStream
                }, fileName));
            }

            return result;
        }

        #endregion

        public void Save(Stream output, List<IArchiveFile> files)
        {
            var darcTreeBuilder = new DarcTreeBuilder(Encoding.Unicode);
            darcTreeBuilder.Build(files.Cast<DarcArchiveFile>().ToArray());

            var entries = darcTreeBuilder.Entries;
            var nameStream = darcTreeBuilder.NameStream;

            var namePosition = HeaderSize_ + entries.Count * EntrySize_;

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
                if (afi.FilePath.GetExtensionWithDot() == ".bclim"
                    || afi.FilePath.GetExtensionWithDot() == ".arc"
                    || afi.FilePath.GetExtensionWithDot() == ".snd")
                    alignment = 0x80;

                bw.WriteAlignment(alignment);
                var fileOffset = (int)bw.BaseStream.Position;

                var writtenSize = afi.WriteFileData(bw.BaseStream);

                darcEntry.offset = fileOffset;
                darcEntry.size = (int)writtenSize;
            }

            // Write entries
            bw.BaseStream.Position = HeaderSize_;
            WriteEntries(entries.Select(x => x.Item1).ToArray(), bw);

            // Write header
            var header = new DarcHeader
            {
                magic = "darc",
                byteOrder = (ushort)_byteOrder,
                headerSize = 0x1C,
                version = 0x1000000,
                fileSize = (int)bw.BaseStream.Length,
                tableOffset = 0x1C,
                tableLength = entries.Count * EntrySize_ + (int)nameStream.Length,
                dataOffset = entries.Where(x => x.Item2 != null).Select(x => x.Item1.offset).Min()
            };

            bw.BaseStream.Position = 0;
            WriteHeader(header, bw);
        }

        private DarcHeader ReadHeader(BinaryReaderX reader)
        {
            return new DarcHeader
            {
                magic = reader.ReadString(4),
                byteOrder = reader.ReadUInt16(),
                headerSize = reader.ReadInt16(),
                version = reader.ReadInt32(),
                fileSize = reader.ReadInt32(),
                tableOffset = reader.ReadInt32(),
                tableLength = reader.ReadInt32(),
                dataOffset = reader.ReadInt32()
            };
        }

        private DarcEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new DarcEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private DarcEntry ReadEntry(BinaryReaderX reader)
        {
            return new DarcEntry
            {
                tmp1 = reader.ReadInt32(),
                offset = reader.ReadInt32(),
                size = reader.ReadInt32()
            };
        }

        private void WriteHeader(DarcHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);

            writer.ByteOrder = ByteOrder.BigEndian;
            writer.Write(header.byteOrder);

            writer.ByteOrder = (ByteOrder)header.byteOrder;
            writer.Write(header.headerSize);
            writer.Write(header.version);
            writer.Write(header.fileSize);
            writer.Write(header.tableOffset);
            writer.Write(header.tableLength);
            writer.Write(header.dataOffset);
        }

        private void WriteEntries(DarcEntry[] entries, BinaryWriterX writer)
        {
            foreach (DarcEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(DarcEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.tmp1);
            writer.Write(entry.offset);
            writer.Write(entry.size);
        }
    }
}
