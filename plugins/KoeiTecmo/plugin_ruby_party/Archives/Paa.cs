using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;

namespace plugin_ruby_party.Archives
{
    class Paa
    {
        private static readonly int EntrySize = 0x10;

        private PaaHeader _header;

        public List<IArchiveFile> Load(Stream binStream, Stream arcStream)
        {
            using var binBr = new BinaryReaderX(binStream);

            // Read header
            _header = ReadHeader(binBr);

            // Read entries
            binStream.Position = _header.entryOffset;
            var entries = ReadEntries(binBr, _header.fileCount);

            // Read offsets
            binStream.Position = _header.offsetsOffset;
            var offsets = ReadOffsets(binBr, _header.fileCount);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < _header.fileCount; i++)
            {
                var entry = entries[i];
                var offset = offsets[i];

                var subStream = new SubStream(arcStream, offset, entry.size);

                binStream.Position = entry.nameOffset;
                var fileName = binBr.ReadNullTerminatedString();

                result.Add(new PaaArchiveFile(new ArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = subStream
                }, entry));
            }

            return result;
        }

        public void Save(Stream binOutput, Stream arcOutput, IList<IArchiveFile> files)
        {
            using var binBw = new BinaryWriterX(binOutput);
            using var arcBw = new BinaryWriterX(arcOutput);

            // Calculate offsets
            var fileOffset = 0x10;
            var entryOffset = 0x20;
            var offsetsOffset = entryOffset + files.Count * EntrySize;
            var stringOffset = offsetsOffset + ((files.Count * 4 + 0xF) & ~0xF);

            // Write files
            var offsets = new List<int>();
            var entries = new List<PaaEntry>();

            var filePosition = fileOffset;
            var stringPosition = stringOffset;
            foreach (var file in files.Cast<PaaArchiveFile>())
            {
                arcOutput.Position = filePosition;
                var writtenSize = file.WriteFileData(arcOutput, true);
                arcBw.WriteAlignment(0x10);

                file.Entry.size = (int)writtenSize;
                file.Entry.nameOffset = stringPosition;

                offsets.Add(filePosition);
                entries.Add(file.Entry);

                filePosition += ((int)writtenSize + 0xF) & ~0xF;
                stringPosition += (file.FilePath.ToRelative().FullName.Length + 1 + 0xF) & ~0xF;
            }

            // Write strings
            binOutput.Position = stringOffset;
            foreach (var file in files)
            {
                binBw.WriteString(file.FilePath.ToRelative().FullName, Encoding.ASCII);
                binBw.WriteAlignment(0x10);
            }

            // Write offsets
            binOutput.Position = offsetsOffset;
            WriteOffsets(offsets, binBw);

            // Write entries
            binOutput.Position = entryOffset;
            WriteEntries(entries, binBw);

            // Write header
            binOutput.Position = 0;

            _header.fileCount = files.Count;
            _header.entryOffset = entryOffset;
            _header.offsetsOffset = offsetsOffset;
            _header.unk2 = _header.fileCount / 2;
            WriteHeader(_header, binBw);
        }

        private PaaHeader ReadHeader(BinaryReaderX reader)
        {
            return new PaaHeader
            {
                magic = reader.ReadString(4),
                unk1 = reader.ReadInt32(),
                fileCount = reader.ReadInt32(),
                entryOffset = reader.ReadInt32(),
                offsetsOffset = reader.ReadInt32(),
                unk2 = reader.ReadInt32()
            };
        }

        private PaaEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new PaaEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private PaaEntry ReadEntry(BinaryReaderX reader)
        {
            return new PaaEntry
            {
                nameOffset = reader.ReadInt32(),
                size = reader.ReadInt32(),
                unk1 = reader.ReadInt32(),
                unk2 = reader.ReadInt32()
            };
        }

        private int[] ReadOffsets(BinaryReaderX reader, int count)
        {
            var result = new int[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadInt32();

            return result;
        }

        private void WriteHeader(PaaHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.unk1);
            writer.Write(header.fileCount);
            writer.Write(header.entryOffset);
            writer.Write(header.offsetsOffset);
            writer.Write(header.unk2);
        }

        private void WriteEntries(IList<PaaEntry> entries, BinaryWriterX writer)
        {
            foreach (PaaEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(PaaEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.nameOffset);
            writer.Write(entry.size);
            writer.Write(entry.unk1);
            writer.Write(entry.unk2);
        }

        private void WriteOffsets(IList<int> entries, BinaryWriterX writer)
        {
            foreach (int entry in entries)
                writer.Write(entry);
        }
    }
}
