using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;

namespace plugin_sting_entertainment.Archives
{
    class Pck
    {
        private static readonly int HeaderSize = 0xC;
        private static readonly int EntrySize = 0x8;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read name header
            var nameHeader = PckSupport.ReadHeader(br);

            // Read file count
            input.Position = nameHeader.size + HeaderSize;
            var fileCount = br.ReadInt32();

            // Read file names
            input.Position = HeaderSize;
            var nameOffsets = ReadIntegers(br, fileCount);

            // Read entries
            input.Position = nameHeader.size + HeaderSize + 4;
            var entries = ReadEntries(br, fileCount);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < fileCount; i++)
            {
                var entry = entries[i];

                var fileStream = new SubStream(input, entry.offset, entry.size);
                input.Position = nameOffsets[i] + HeaderSize;
                var fileName = br.ReadNullTerminatedString();

                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = fileStream
                }));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var nameOffsetsOffset = HeaderSize;
            var stringOffset = nameOffsetsOffset + files.Count * 4;
            var packOffset = (stringOffset + files.Sum(x => x.FilePath.ToRelative().FullName.Length + 1) + 3) & ~3;
            var entryOffset = packOffset + HeaderSize + 4;
            var dataOffset = (entryOffset + files.Count * EntrySize + 0x7FF) & ~0x7FF;

            // Write files
            var names = new List<string>();
            var entries = new List<PckEntry>();

            var dataPosition = dataOffset;
            foreach (var file in files)
            {
                // Write file data
                output.Position = dataPosition;
                var writtenSize = file.WriteFileData(output);
                bw.WriteAlignment(0x800);

                // Add entry
                entries.Add(new PckEntry { offset = dataPosition, size = (int)writtenSize });

                // Add name
                names.Add(file.FilePath.ToRelative().FullName);

                dataPosition = (int)((dataPosition + writtenSize + 0x7FF) & ~0x7FF);
            }
            bw.Write(0);

            // Write entries
            output.Position = entryOffset;
            WriteEntries(entries, bw);

            // Write pack header
            output.Position = packOffset;
            PckSupport.WriteHeader(new PckHeader { magic = "Pack    ", size = HeaderSize + 4 + files.Count * EntrySize }, bw);
            bw.Write(entries.Count);

            // Write strings
            var nameOffsets = new List<int>();

            output.Position = stringOffset;
            foreach (var name in names)
            {
                nameOffsets.Add((int)output.Position - HeaderSize);
                bw.WriteString(name, Encoding.ASCII);
            }

            // Write name offsets
            output.Position = nameOffsetsOffset;
            WriteIntegers(nameOffsets, bw);

            // Write name header
            output.Position = 0;
            PckSupport.WriteHeader(new PckHeader { magic = "Filename", size = packOffset }, bw);
        }

        private int[] ReadIntegers(BinaryReaderX reader, int count)
        {
            var result = new int[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadInt32();

            return result;
        }

        private PckEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new PckEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private PckEntry ReadEntry(BinaryReaderX reader)
        {
            return new PckEntry
            {
                offset = reader.ReadInt32(),
                size = reader.ReadInt32()
            };
        }

        private void WriteIntegers(IList<int> entries, BinaryWriterX writer)
        {
            foreach (int entry in entries)
                writer.Write(entry);
        }

        private void WriteEntries(IList<PckEntry> entries, BinaryWriterX writer)
        {
            foreach (PckEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(PckEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.offset);
            writer.Write(entry.size);
        }
    }
}
