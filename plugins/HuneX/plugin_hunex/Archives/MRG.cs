using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_hunex.Archives
{
    // Specifications: https://github.com/Hintay/PS-HuneX_Tools/tree/master/Specifications
    class MRG
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(MRGHeader));
        private static readonly int EntrySize = Tools.MeasureType(typeof(MRGEntry));

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = br.ReadType<MRGHeader>();

            // Read entries
            var entries = br.ReadMultiple<MRGEntry>(header.fileCount);

            // Add files
            var dataOffset = br.BaseStream.Position;

            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < header.fileCount; i++)
            {
                var entry = entries[i];

                var subStream = new SubStream(input, dataOffset + entry.Offset, entry.Size);
                var fileName = $"{i:00000000}.bin";

                result.Add(new ArchiveFileInfo(subStream, fileName));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = HeaderSize;
            var fileOffset = entryOffset + files.Count * EntrySize;

            // Write files
            var entries = new List<MRGEntry>();

            var filePosition = fileOffset;
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                output.Position = filePosition;
                var writtenSize = file.SaveFileData(output);
                bw.WritePadding(8, 0xFF);

                entries.Add(new MRGEntry
                {
                    Offset = filePosition - fileOffset,
                    Size = (int)writtenSize
                });

                filePosition += (int)writtenSize + 8;
            }

            // Write entries
            output.Position = entryOffset;
            bw.WriteMultiple(entries);

            // Write header
            output.Position = 0;
            bw.WriteType(new MRGHeader
            {
                fileCount = (short)files.Count
            });
        }
    }
}
