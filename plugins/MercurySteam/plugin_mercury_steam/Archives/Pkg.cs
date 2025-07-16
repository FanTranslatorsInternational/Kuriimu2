using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_mercury_steam.Archives
{
    class Pkg
    {
        private static readonly int HeaderSize = 0xC;
        private static readonly int EntrySize = 0xC;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = ReadHeader(br);

            // Read entries
            var entries = ReadEntries(br, header.fileCount);

            // Add files
            var result = new List<IArchiveFile>();
            foreach (var entry in entries)
            {
                var name = $"{entry.hash:X8}.bin";
                var fileStream = new SubStream(input, entry.startOffset, entry.endOffset - entry.startOffset);

                result.Add(new PkgArchiveFile(new ArchiveFileInfo
                {
                    FilePath = name,
                    FileData = fileStream
                }, entry.hash));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var dataOffset = (entryOffset + files.Count * EntrySize + 0x7F) & ~0x7F;

            // Write files
            var entries = new List<PkgEntry>();

            var dataPosition = dataOffset;
            foreach (var file in files.Cast<PkgArchiveFile>())
            {
                // Write file data
                var alignment = PkgSupport.DetermineAlignment(file.Type);
                var alignedDataPosition = (dataPosition + alignment - 1) & ~(alignment - 1);

                output.Position = alignedDataPosition;
                var writtenSize = file.WriteFileData(output, true);

                // Create entry
                entries.Add(new PkgEntry
                {
                    startOffset = alignedDataPosition,
                    endOffset = (int)(alignedDataPosition + writtenSize),
                    hash = file.Hash
                });

                dataPosition = (int)(alignedDataPosition + writtenSize);
            }
            bw.WriteAlignment(4);

            // Write entries
            output.Position = entryOffset;
            WriteEntries(entries, bw);

            // Write header
            var header = new PkgHeader
            {
                fileCount = files.Count,
                tableSize = dataOffset - 4,
                dataSize = (int)(output.Length - dataOffset)
            };

            output.Position = 0;
            WriteHeader(header, bw);
        }

        private PkgHeader ReadHeader(BinaryReaderX reader)
        {
            return new PkgHeader
            {
                tableSize = reader.ReadInt32(),
                dataSize = reader.ReadInt32(),
                fileCount = reader.ReadInt32()
            };
        }

        private PkgEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new PkgEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private PkgEntry ReadEntry(BinaryReaderX reader)
        {
            return new PkgEntry
            {
                hash = reader.ReadUInt32(),
                startOffset = reader.ReadInt32(),
                endOffset = reader.ReadInt32()
            };
        }

        private void WriteHeader(PkgHeader header, BinaryWriterX writer)
        {
            writer.Write(header.tableSize);
            writer.Write(header.dataSize);
            writer.Write(header.fileCount);
        }

        private void WriteEntries(IList<PkgEntry> entries, BinaryWriterX writer)
        {
            foreach (PkgEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(PkgEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.hash);
            writer.Write(entry.startOffset);
            writer.Write(entry.endOffset);
        }
    }
}
