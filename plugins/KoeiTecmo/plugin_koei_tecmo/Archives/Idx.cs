using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_koei_tecmo.Archives
{
    class Idx
    {
        public List<IArchiveFile> Load(Stream idxStream, Stream binStream)
        {
            using var br = new BinaryReaderX(idxStream);
            using var binBr = new BinaryReaderX(binStream, true);

            // Read entries
            var entries = ReadEntries(br, (int)(idxStream.Length / 8));

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];

                var fileStream = new SubStream(binStream, entry.offset, entry.size);
                var fileName = $"{i:00000000}{IdxSupport.DetermineExtension(fileStream)}";

                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = fileStream
                }));
            }

            return result;
        }

        public void Save(Stream idxStream, Stream binStream, IList<IArchiveFile> files)
        {
            // Write files
            var entries = new List<IdxEntry>();

            var dataPosition = 0;
            foreach (var file in files)
            {
                // Write file data
                binStream.Position = dataPosition;
                var writtenSize = file.WriteFileData(binStream);

                // Add entry
                entries.Add(new IdxEntry { offset = dataPosition, size = (int)writtenSize });

                dataPosition += (int)writtenSize;
            }

            // Write entries
            using var bw = new BinaryWriterX(idxStream);
            WriteEntries(entries, bw);
        }

        private IdxEntry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new IdxEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private IdxEntry ReadEntry(BinaryReaderX reader)
        {
            return new IdxEntry
            {
                size = reader.ReadInt32(),
                offset = reader.ReadInt32()
            };
        }

        private void WriteEntries(IList<IdxEntry> entries, BinaryWriterX writer)
        {
            foreach (IdxEntry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(IdxEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.size);
            writer.Write(entry.offset);
        }
    }
}
