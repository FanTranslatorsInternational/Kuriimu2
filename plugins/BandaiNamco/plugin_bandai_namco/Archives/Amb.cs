using Komponent.Contract.Enums;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_bandai_namco.Archives
{
    class Amb
    {
        private static readonly int HeaderSize = 0x20;
        private static readonly int FileEntrySize = 0x10;

        private ByteOrder _byteOrder;
        private AmbHeader _header;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Determine byte order (by determining header length == 0x20)
            input.Position = 4;
            _byteOrder = br.ByteOrder = br.ReadByte() == 0x20 ? ByteOrder.LittleEndian : ByteOrder.BigEndian;

            // Read header
            input.Position = 0;
            _header = ReadHeader(br);

            // Read entries
            br.BaseStream.Position = _header.fileEntryStart;
            var entries = ReadEntries(br, _header.fileCount);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < entries.Length; i++)
            {
                var subStream = new SubStream(input, entries[i].offset, entries[i].size);
                var name = $"{i:00000000}{AmbSupport.DetermineExtension(subStream)}";

                result.Add(new AmbArchiveFile(new ArchiveFileInfo
                {
                    FilePath = name,
                    FileData = subStream
                }, entries[i]));
            }

            return result;
        }

        public void Save(Stream output, List<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output, _byteOrder);

            // Calculate offsets
            var fileEntryOffset = HeaderSize;
            var dataOffset = (fileEntryOffset + files.Count * FileEntrySize + 0x7F) & ~0x7F;

            // Write files
            var fileEntries = new List<AmbFileEntry>();

            bw.BaseStream.Position = dataOffset;
            foreach (var file in files.Cast<AmbArchiveFile>())
            {
                var fileOffset = bw.BaseStream.Position;
                var writtenSize = file.WriteFileData(bw.BaseStream, true);
                bw.WriteAlignment(0x80);

                fileEntries.Add(new AmbFileEntry
                {
                    offset = (int)fileOffset,
                    size = (int)writtenSize,
                    unk1 = file.Entry.unk1
                });
            }

            bw.WriteAlignment(0x80);

            // Write file entries
            bw.BaseStream.Position = fileEntryOffset;
            WriteEntries(fileEntries, bw);

            // Write header
            var header = new AmbHeader
            {
                fileEntryStart = fileEntryOffset,
                dataOffset = dataOffset,
                fileCount = files.Count,
                unk1 = _header.unk1
            };

            bw.BaseStream.Position = 0;
            WriteHeader(header, bw);
        }

        private AmbHeader ReadHeader(BinaryReaderX reader)
        {
            return new AmbHeader
            {
                magic = reader.ReadString(4),
                headerLength = reader.ReadInt32(),
                zero0 = reader.ReadInt32(),
                unk1 = reader.ReadInt32(),
                fileCount = reader.ReadInt32(),
                fileEntryStart = reader.ReadInt32(),
                dataOffset = reader.ReadInt32(),
                zero1 = reader.ReadInt32()
            };
        }

        private AmbFileEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new AmbFileEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private AmbFileEntry ReadEntry(BinaryReaderX reader)
        {
            return new AmbFileEntry
            {
                offset = reader.ReadInt32(),
                size = reader.ReadInt32(),
                unk1 = reader.ReadInt32(),
                zero0 = reader.ReadInt32()
            };
        }

        private void WriteHeader(AmbHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.headerLength);
            writer.Write(header.zero0);
            writer.Write(header.unk1);
            writer.Write(header.fileCount);
            writer.Write(header.fileEntryStart);
            writer.Write(header.dataOffset);
            writer.Write(header.zero1);
        }

        private void WriteEntries(IList<AmbFileEntry> entries, BinaryWriterX writer)
        {
            foreach (AmbFileEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(AmbFileEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.offset);
            writer.Write(entry.size);
            writer.Write(entry.unk1);
            writer.Write(entry.zero0);
        }
    }
}
