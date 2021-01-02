using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_inti_creates.Archives
{
    class Fnt
    {
        private static readonly int FileEntrySize = Tools.MeasureType(typeof(FntFileEntry));

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            var br = new BinaryReaderX(input, true);

            // Read entries
            var fileCount = br.ReadInt32();
            var entries = br.ReadMultiple<FntFileEntry>(fileCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < entries.Count; i++)
            {
                var subStream = new SubStream(input, entries[i].offset, entries[i].endOffset - entries[i].offset);
                var name = $"{i:00000000}{FntSupport.DetermineExtension(subStream)}";

                result.Add(new FntArchiveFileInfo(subStream, name));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var dataOffset = (4 + files.Count * FileEntrySize + 0x7F) & ~0x7F;

            // Write files
            var entries = new List<FntFileEntry>();

            output.Position = dataOffset;
            foreach (var file in files.Cast<FntArchiveFileInfo>())
            {
                var fileOffset = output.Position;
                var writtenSize = file.SaveFileData(output);

                entries.Add(new FntFileEntry
                {
                    offset = (int)fileOffset,
                    endOffset = (int)(fileOffset + writtenSize)
                });
            }

            // Write entries
            bw.BaseStream.Position = 0;
            bw.Write(files.Count);
            bw.WriteMultiple(entries);
        }
    }
}
