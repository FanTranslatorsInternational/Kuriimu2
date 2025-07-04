using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;
using System.Reflection.PortableExecutable;

namespace plugin_yuusha_shisu.Archives
{
    public class Pac
    {
        private const int EntryAlignment = 0x20;
        private const int FileAlignment = 0x80;

        private FileHeader _header;
        private IList<FileEntry> _entries;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Header
            _header = ReadHeader(br);

            // Offsets
            var offsets = ReadOffsets(br, _header.FileCount);
            br.SeekAlignment(EntryAlignment);

            // Entries
            _entries = ReadEntries(br, _header.FileCount);

            // Files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < offsets.Length; i++)
            {
                br.BaseStream.Position = offsets[i];
                var length = br.ReadInt32();
                var off = br.BaseStream.Position + FileAlignment - sizeof(int);

                // TODO: Add plugin Id to each *.msg file
                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = _entries[i].FileName.Trim('\0'),
                    FileData = new SubStream(br.BaseStream, off, length)
                }));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output, true);

            // Header
            WriteHeader(_header, bw);
            var offsetPosition = bw.BaseStream.Position;

            // Skip Offsets
            bw.BaseStream.Position += _header.FileCount * sizeof(int);
            bw.WriteAlignment(EntryAlignment);

            // Entries
            WriteEntries(_entries, bw);
            bw.WriteAlignment(FileAlignment);

            // Files
            var offsets = new List<int>();
            foreach (var afi in files)
            {
                offsets.Add((int)bw.BaseStream.Position);

                bw.Write((int)afi.FileSize);
                bw.Write(FileAlignment);
                bw.WriteAlignment(FileAlignment);

                afi.WriteFileData(bw.BaseStream);
                bw.WriteAlignment(FileAlignment);
            }

            // Offsets
            bw.BaseStream.Position = offsetPosition;
            WriteOffsets(offsets, bw);
        }

        private FileHeader ReadHeader(BinaryReaderX reader)
        {
            return new FileHeader
            {
                Magic = reader.ReadString(4),
                Unk1 = reader.ReadInt32(),
                FileCount = reader.ReadInt32(),
                Null1 = reader.ReadInt32(),
                ArchiveName = reader.ReadString(0x20)
            };
        }

        private FileEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new FileEntry[count];

            for (var i = 0; i < count; i++)
            {
                result[i] = ReadEntry(reader);
                reader.SeekAlignment(0x20);
            }

            return result;
        }

        private FileEntry ReadEntry(BinaryReaderX reader)
        {
            var entry = new FileEntry
            {
                Extension = reader.ReadString(4),
                Unk1 = reader.ReadInt16(),
                FileNumbers = reader.ReadInt16(),
                Checksum = reader.ReadInt32(),
                Unk2 = reader.ReadInt16(),
                StringLength = reader.ReadInt16(),
                Null2 = reader.ReadInt32()
            };

            entry.FileName = reader.ReadString(entry.StringLength);

            return entry;
        }

        private int[] ReadOffsets(BinaryReaderX reader, int count)
        {
            var result = new int[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadInt32();

            return result;
        }

        private void WriteHeader(FileHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.Magic, writeNullTerminator: false);
            writer.Write(header.Unk1);
            writer.Write(header.FileCount);
            writer.Write(header.Null1);
            writer.WriteString(header.ArchiveName, writeNullTerminator: false);
        }

        private void WriteEntries(IList<FileEntry> entries, BinaryWriterX writer)
        {
            foreach (FileEntry entry in entries)
            {
                WriteEntry(entry, writer);
                writer.WriteAlignment(0x20);
            }
        }

        private void WriteEntry(FileEntry entry, BinaryWriterX writer)
        {
            writer.WriteString(entry.Extension, writeNullTerminator: false);
            writer.Write(entry.Unk1);
            writer.Write(entry.FileNumbers);
            writer.Write(entry.Checksum);
            writer.Write(entry.Unk2);
            writer.Write(entry.StringLength);
            writer.Write(entry.Null2);
            writer.WriteString(entry.FileName, writeNullTerminator: false);
        }

        private void WriteOffsets(IList<int> offsets, BinaryWriterX writer)
        {
            foreach (int offset in offsets)
                writer.Write(offset);
        }
    }
}
