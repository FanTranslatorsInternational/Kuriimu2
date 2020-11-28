using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_beeworks.Archives
{
    class TD1
    {
        private static readonly int EntrySize = Tools.MeasureType(typeof(TD1Entry));

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read file count
            var fileCount = br.ReadInt32();

            // Read entries
            var entries = br.ReadMultiple<TD1Entry>(fileCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < fileCount; i++)
            {
                var entry = entries[i];

                var subStream = new SubStream(input, entry.offset << 2, entry.size);
                var fileName = $"{i:00000000}.bin";

                result.Add(new ArchiveFileInfo(subStream, fileName));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = 4;
            var fileOffset = entryOffset + files.Count * EntrySize;

            // Write files
            var entries = new List<TD1Entry>();

            var filePosition = fileOffset;
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                output.Position = filePosition;
                var writtenSize = file.SaveFileData(output);

                entries.Add(new TD1Entry
                {
                    offset = filePosition >> 2,
                    size = (int)writtenSize
                });

                filePosition += (int)((writtenSize + 3) & ~3);
            }

            // Write entries
            output.Position = entryOffset;
            bw.WriteMultiple(entries);

            // Write file count
            output.Position = 0;
            bw.WriteType(files.Count);
        }
    }
}
