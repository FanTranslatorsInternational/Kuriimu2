using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;

namespace plugin_beeworks.Archives
{
    class TD3
    {
        private static readonly int HeaderSize = 0x10;
        private static readonly int EntrySize = 0x8;

        private int _bufferSize;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = ReadHeader(br);
            br.SeekAlignment();

            _bufferSize = header.nameBufSize;

            // Read entries
            var entries = ReadEntries(br, header.fileCount, header.nameBufSize);

            // Add files
            var result = new List<IArchiveFile>();
            foreach (var entry in entries)
            {
                var subStream = new SubStream(input, entry.offset, entry.size);
                var fileName = entry.fileName.Trim('\0');

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
            var entryOffset = HeaderSize;
            var fileOffset = (entryOffset + files.Count * (EntrySize + _bufferSize) + 0xF) & ~0xF;

            // Write files
            var entries = new List<TD3Entry>();

            var filePosition = fileOffset;
            foreach (var file in files)
            {
                output.Position = filePosition;
                var writtenSize = file.WriteFileData(output);

                entries.Add(new TD3Entry
                {
                    offset = filePosition,
                    size = (int)writtenSize,
                    fileName = file.FilePath.ToRelative().FullName.PadRight(_bufferSize, '\0')
                });

                filePosition += ((int)writtenSize + 0xF) & ~0xF;
            }

            // Write entries
            output.Position = entryOffset;
            WriteEntries(entries, bw);

            // Write header
            var header = new TD3Header
            {
                fileCount = files.Count,
                nameBufSize = _bufferSize
            };

            output.Position = 0;
            WriteHeader(header, bw);
        }

        private TD3Header ReadHeader(BinaryReaderX reader)
        {
            return new TD3Header
            {
                fileCount = reader.ReadInt32(),
                nameBufSize = reader.ReadInt32()
            };
        }

        private TD3Entry[] ReadEntries(BinaryReaderX reader, int count, int bufferSize)
        {
            var result = new TD3Entry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader, bufferSize);

            return result;
        }

        private TD3Entry ReadEntry(BinaryReaderX reader, int bufferSize)
        {
            return new TD3Entry
            {
                offset = reader.ReadInt32(),
                size = reader.ReadInt32(),
                fileName = reader.ReadString(bufferSize)
            };
        }

        private void WriteHeader(TD3Header header, BinaryWriterX writer)
        {
            writer.Write(header.fileCount);
            writer.Write(header.nameBufSize);
        }

        private void WriteEntries(IList<TD3Entry> entries, BinaryWriterX writer)
        {
            foreach (TD3Entry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(TD3Entry entry, BinaryWriterX writer)
        {
            writer.Write(entry.offset);
            writer.Write(entry.size);
            writer.WriteString(entry.fileName, writeNullTerminator: false);
        }
    }
}
