using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_capcom.Archives
{
    /*
     * List of games/updates that use this format on Mobile:
     * DGS1 v1.00.00, v1.00.01
     * DGS1 v1.00.02 has content differences
     * DGS2
     * Dual Destinies
     * Spirit of Justice
     */

    // HINT: This format is used for all 3D Ace Attorney games on mobile platforms
    // HINT: Those games have 2 OBB's, one for videos and one for assets
    // HINT: The video OBB is a normal zip, while the asset OBB is of this format
    class Obb
    {
        private static readonly int HeaderSize = 0x10;
        private static readonly int EntrySize = 0x10;

        private ObbHeader _header;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = ReadHeader(br);

            // Read entries
            var entries = ReadEntries(br, _header.fileCount);

            // Add files
            var result = new List<IArchiveFile>();
            foreach (var entry in entries)
            {
                var subStream = new SubStream(input, entry.offset, entry.size);
                var fileName = $"{entry.pathHash:X8}{ObbSupport.DetermineExtension(subStream)}";

                result.Add(new ObbArchiveFile(new ArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = subStream
                }, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var fileOffset = entryOffset + files.Count * EntrySize;

            // Write files
            var entries = new List<ObbEntry>();

            var filePosition = fileOffset;
            foreach (var file in files.Cast<ObbArchiveFile>())
            {
                output.Position = filePosition;
                var writtenSize = file.WriteFileData(output, true);

                file.Entry.offset = filePosition;
                file.Entry.size = (int)writtenSize;
                entries.Add(file.Entry);

                filePosition += (int)writtenSize;
            }

            // Write entries
            output.Position = entryOffset;
            WriteEntries(entries, bw);

            // Write header
            output.Position = 0;

            _header.fileCount = files.Count;
            WriteHeader(_header, bw);
        }

        private ObbHeader ReadHeader(BinaryReaderX reader)
        {
            return new ObbHeader
            {
                magic = reader.ReadString(4),
                version = reader.ReadInt32(),
                fileCount = reader.ReadInt32(),
                crc = reader.ReadUInt32()
            };
        }

        private ObbEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new ObbEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private ObbEntry ReadEntry(BinaryReaderX reader)
        {
            return new ObbEntry
            {
                pathHash = reader.ReadUInt32(),
                offset = reader.ReadInt32(),
                size = reader.ReadInt32(),
                unkHash = reader.ReadUInt32()
            };
        }

        private void WriteHeader(ObbHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.version);
            writer.Write(header.fileCount);
            writer.Write(header.crc);
        }

        private void WriteEntries(IList<ObbEntry> entries, BinaryWriterX writer)
        {
            foreach (ObbEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(ObbEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.pathHash);
            writer.Write(entry.offset);
            writer.Write(entry.size);
            writer.Write(entry.unkHash);
        }
    }
}
