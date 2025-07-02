using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;

namespace plugin_spike_chunsoft.Archives
{
    class Zdp
    {
        private static readonly int PartitionHeaderSize = 0x10;
        private static readonly int HeaderSize = 0x1C;
        private static readonly int EntrySize = 0x8;

        private ZdpPartitionHeader _partitionHeader;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read headers
            _partitionHeader = ReadPartitionHeader(br);
            var header = ReadHeader(br);

            // Read entries
            var entries = ReadEntries(br, header.entryCount);
            var nameOffsets = ReadNameOffsets(br, header.nameOffsetCount);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < header.fileCount; i++)
            {
                var entry = entries[i];
                var nameOffset = nameOffsets[i];

                var subStream = new SubStream(input, entry.offset, entry.size);

                input.Position = nameOffset;
                var fileName = br.ReadNullTerminatedString();

                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = subStream
                }));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var headerOffset = PartitionHeaderSize;
            var entryOffset = headerOffset + HeaderSize;
            var nameOffsetsOffset = entryOffset + files.Count * EntrySize;
            var fileOffset = (nameOffsetsOffset + files.Count * 4 + 0x7F) & ~0x7F;

            // Write files
            var entries = new List<ZdpFileEntry>();

            var filePosition = fileOffset;
            foreach (var file in files)
            {
                output.Position = filePosition;
                var writtenSize = file.WriteFileData(output);

                entries.Add(new ZdpFileEntry
                {
                    offset = filePosition,
                    size = (int)writtenSize
                });

                filePosition += (int)writtenSize;
            }

            // Write strings
            var stringOffset = (int)output.Length;

            var stringPosition = stringOffset;
            var nameOffsets = new List<int>();
            foreach (var file in files)
            {
                nameOffsets.Add(stringPosition);
                output.Position = stringPosition;
                bw.WriteString(file.FilePath.ToRelative().GetName(), Encoding.ASCII);

                stringPosition = (int)output.Position;
            }

            // Write name offsets
            output.Position = nameOffsetsOffset;
            WriteNameOffsets(nameOffsets, bw);

            // Write entries
            output.Position = entryOffset;
            WriteEntries(entries, bw);

            // Write header
            var header = new ZdpHeader
            {
                fileCount = files.Count,
                entryCount = (short)entries.Count,
                nameOffsetCount = (short)nameOffsets.Count,
                stringCount = files.Count,
                entryOffset = entryOffset,
                nameOffsetsOffset = nameOffsetsOffset
            };

            output.Position = headerOffset;
            WriteHeader(header, bw);

            // Write partition header
            output.Position = 0;
            WritePartitionHeader(_partitionHeader, bw);
        }

        private ZdpPartitionHeader ReadPartitionHeader(BinaryReaderX reader)
        {
            return new ZdpPartitionHeader
            {
                magic = reader.ReadString(8),
                zero0 = reader.ReadInt32(),
                unk1 = reader.ReadInt32()
            };
        }

        private ZdpHeader ReadHeader(BinaryReaderX reader)
        {
            return new ZdpHeader
            {
                headerSize = reader.ReadInt32(),
                fileCount = reader.ReadInt32(),
                entryCount = reader.ReadInt16(),
                nameOffsetCount = reader.ReadInt16(),
                stringCount = reader.ReadInt32(),
                entryOffset = reader.ReadInt32(),
                nameOffsetsOffset = reader.ReadInt32(),
                zero0 = reader.ReadInt32()
            };
        }

        private ZdpFileEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new ZdpFileEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private ZdpFileEntry ReadEntry(BinaryReaderX reader)
        {
            return new ZdpFileEntry
            {
                offset = reader.ReadInt32(),
                size = reader.ReadInt32()
            };
        }

        private int[] ReadNameOffsets(BinaryReaderX reader, int count)
        {
            var result = new int[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadInt32();

            return result;
        }

        private void WritePartitionHeader(ZdpPartitionHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.zero0);
            writer.Write(header.unk1);
        }

        private void WriteHeader(ZdpHeader header, BinaryWriterX writer)
        {
            writer.Write(header.headerSize);
            writer.Write(header.fileCount);
            writer.Write(header.entryCount);
            writer.Write(header.nameOffsetCount);
            writer.Write(header.stringCount);
            writer.Write(header.entryOffset);
            writer.Write(header.nameOffsetsOffset);
            writer.Write(header.zero0);
        }

        private void WriteEntries(IList<ZdpFileEntry> entries, BinaryWriterX writer)
        {
            foreach (ZdpFileEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(ZdpFileEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.offset);
            writer.Write(entry.size);
        }

        private void WriteNameOffsets(IList<int> offsets, BinaryWriterX writer)
        {
            foreach (int offset in offsets)
                writer.Write(offset);
        }
    }
}
