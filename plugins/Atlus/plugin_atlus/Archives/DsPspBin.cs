using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_atlus.Archives
{
    class DsPspBin
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(DsPspBinHeader));
        private static readonly int PointerSize = Tools.MeasureType(typeof(DsPspBinEntry));

        private DsPspBinHeader _header;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read fileCount
            _header = br.ReadType<DsPspBinHeader>();

            // Read pointers
            int entryPosition = (_header.FileCount * PointerSize) + 0x8;
            var entries = br.ReadMultiple<DsPspBinEntry>(_header.FileCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (int i = 0; i < _header.FileCount; i++)
            {
                var fileStream = new SubStream(input, entryPosition, entries[i].Size);
                string name = i.ToString("X8") + ".bin";
                entryPosition += entries[i].Size;
                result.Add(new ArchiveFileInfo(fileStream, name));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculations
            int entryPosition = (_header.FileCount * PointerSize) + 0x8;

            // Write fileCount
            var header = new DsPspBinHeader { FileCount = files.Count };
            bw.WriteType(header);

            // Write sizes
            var entries = new List<DsPspBinEntry>();

            output.Position = entryPosition;
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                var writtenSize = file.SaveFileData(output);

                entries.Add(new DsPspBinEntry
                {                    
                    Size = (int)writtenSize
                });                
            }

            // Write pointers
            output.Position = 0x4;
            bw.WriteMultiple<DsPspBinEntry>(entries);
        }
    }
}
