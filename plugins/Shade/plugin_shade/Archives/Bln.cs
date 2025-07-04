using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_shade.Archives
{
    // Game: Inazuma Eleven GO Strikers 2013
    // HINT: Despite being on Wii, this archive is Little Endian
    class Bln
    {
        private byte[] _unkIndexData;

        public List<IArchiveFile> Load(Stream indexStream, Stream dataStream)
        {
            // Read index entries from mcb0
            var indexEntryCount = PeekMcb0EntryCount(indexStream);

            using var indexBr = new BinaryReaderX(indexStream);
            var indexEntries = ReadEntries(indexBr, indexEntryCount);

            // Save unknown data from the index file
            _unkIndexData = indexBr.ReadBytes((int)(indexBr.BaseStream.Length - indexBr.BaseStream.Position));

            // Parse files from mcb1
            var index = 0;
            var result = new List<IArchiveFile>();
            foreach (var indexEntry in indexEntries)
            {
                var stream = new SubStream(dataStream, indexEntry.offset, indexEntry.size);
                result.Add(new BlnArchiveFile(new ArchiveFileInfo
                {
                    FilePath = $"{index++:D8}_{indexEntry.id:X4}.bin",
                    FileData = stream,
                    PluginIds = [Guid.Parse("6d71d07c-b517-496b-b659-3498cd3542fd")]
                }, indexEntry));
            }

            return result;
        }

        public void Save(Stream indexOutput, Stream dataOutput, IList<IArchiveFile> files)
        {
            // Write files
            using var indexBw = new BinaryWriterX(indexOutput);

            var offset = 0u;
            foreach (var file in files.Cast<BlnArchiveFile>())
            {
                var dataSize = (uint)file.WriteFileData(dataOutput, true);

                file.Entry.offset = offset;
                file.Entry.size = dataSize;
                WriteEntry(file.Entry, indexBw);

                offset += dataSize;
            }

            // Write unknown data
            indexBw.Write(_unkIndexData);
        }

        private int PeekMcb0EntryCount(Stream indexStream)
        {
            var bkPos = indexStream.Position;
            var count = 0;

            using var br = new BinaryReaderX(indexStream, true);
            while (br.ReadInt32() != 0)
            {
                count++;
                br.BaseStream.Position += 8;
            }

            indexStream.Position = bkPos;
            return count;
        }

        private Mcb0Entry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new Mcb0Entry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private Mcb0Entry ReadEntry(BinaryReaderX reader)
        {
            return new Mcb0Entry
            {
                id = reader.ReadInt16(),
                unk2 = reader.ReadInt16(),
                offset = reader.ReadUInt32(),
                size = reader.ReadUInt32()
            };
        }

        private void WriteEntry(Mcb0Entry entry, BinaryWriterX writer)
        {
            writer.Write(entry.id);
            writer.Write(entry.unk2);
            writer.Write(entry.offset);
            writer.Write(entry.size);
        }
    }
}
