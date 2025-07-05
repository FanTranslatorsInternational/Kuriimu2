using Komponent.IO;
using Komponent.Streams;
using Kompression;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_kadokawa.Archives
{
    class Enc
    {
        private static readonly int FileEntrySize = 0xC;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read entries
            var entryCount = br.ReadInt32();
            var entries = ReadEntries(br, entryCount);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < entryCount; i++)
            {
                var entry = entries[i];

                var subStream = new SubStream(input, entry.offset, entry.size & 0x7FFFFFFF);
                var name = $"{i:00000000}{EncSupport.DetermineExtension(subStream)}";

                result.Add(CreateAfi(subStream, name, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var fileOffset = (4 + files.Count * FileEntrySize + 0x3F) & ~0x3F;

            // Write files
            var entries = new List<EncFileEntry>();

            bw.BaseStream.Position = fileOffset;
            foreach (var file in files)
            {
                fileOffset = (int)bw.BaseStream.Position;
                var writtenSize = file.WriteFileData(output);

                if (file != files.Last())
                    bw.WriteAlignment(0x40);

                var entry = new EncFileEntry
                {
                    offset = fileOffset,
                    size = (uint)writtenSize,
                    decompSize = (int)file.FileSize
                };
                if (file.UsesCompression)
                    entry.size |= 0x80000000;

                entries.Add(entry);
            }

            // Write entries
            bw.BaseStream.Position = 0;

            bw.Write(files.Count);
            WriteEntries(entries, bw);
        }

        private IArchiveFile CreateAfi(Stream file, string name, EncFileEntry entry)
        {
            if (entry.IsCompressed)
                return new ArchiveFile(new CompressedArchiveFileInfo
                {
                    FilePath = name,
                    FileData = file,
                    Compression = Compressions.LzEnc.Build(),
                    DecompressedSize = entry.decompSize
                });

            return new ArchiveFile(new ArchiveFileInfo
            {
                FilePath = name,
                FileData = file,
            });
        }

        private EncFileEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new EncFileEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private EncFileEntry ReadEntry(BinaryReaderX reader)
        {
            return new EncFileEntry
            {
                size = reader.ReadUInt32(),
                decompSize = reader.ReadInt32(),
                offset = reader.ReadInt32()
            };
        }

        private void WriteEntries(IList<EncFileEntry> entries, BinaryWriterX writer)
        {
            foreach (EncFileEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(EncFileEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.size);
            writer.Write(entry.decompSize);
            writer.Write(entry.offset);
        }
    }
}
