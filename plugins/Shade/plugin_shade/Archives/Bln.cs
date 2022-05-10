using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_shade.Archives
{
    // Game: Inazuma Eleven GO Strikers 2013
    // HINT: Despite being on Wii, this archive is Little Endian
    class Bln
    {
        private byte[] _unkIndexData;

        public IList<IArchiveFileInfo> Load(Stream indexStream, Stream dataStream)
        {
            // Read index entries from mcb0
            var indexEntryCount = PeekMcb0EntryCount(indexStream);

            using var indexBr = new BinaryReaderX(indexStream);
            var indexEntries = indexBr.ReadMultiple<Mcb0Entry>(indexEntryCount);

            // Save unknown data from the index file
            _unkIndexData = indexBr.ReadBytes((int)(indexBr.BaseStream.Length - indexBr.BaseStream.Position));

            // Parse files from mcb1
            int index = 0;
            var result = new List<IArchiveFileInfo>();
            foreach (var indexEntry in indexEntries)
            {
                var stream = new SubStream(dataStream, indexEntry.offset, indexEntry.size);
                result.Add(new BlnArchiveFileInfo(stream, $"{index++:D8}_{indexEntry.id:X4}.bin", indexEntry)
                {
                    PluginIds = new[] { Guid.Parse("6d71d07c-b517-496b-b659-3498cd3542fd") }
                });
            }

            return result;
        }

        public void Save(Stream indexOutput, Stream dataOutput, IList<IArchiveFileInfo> files)
        {
            // Write files
            using var indexBw = new BinaryWriterX(indexOutput);

            var offset = 0u;
            foreach (var file in files.Cast<BlnArchiveFileInfo>())
            {
                var dataSize = (uint)file.SaveFileData(dataOutput);

                file.Entry.offset = offset;
                file.Entry.size = dataSize;
                indexBw.WriteType(file.Entry);

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
    }
}
