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
        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read fileCount
            int fileCount = br.ReadInt32();

            // Read pointers
            int entryPosition = fileCount * sizeof(int);
            var sizeList = new List<int>();
            for (int i = 0; i < fileCount; i++)
            {
                sizeList.Add(br.ReadInt32());
            }

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (int i = 0; i < fileCount; i++)
            {
                var fileStream = new SubStream(input, entryPosition, sizeList[i]);
                string name = $"{i:X8}.bin";
                entryPosition += sizeList[i];
                result.Add(new ArchiveFileInfo(fileStream, name));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);
            int fileCount = files.Count;

            // Calculations
            int entryPosition = fileCount * sizeof(int);

            // Write fileCount
            bw.Write(fileCount);

            // Write data
            output.Position = entryPosition;
            var sizeList = new List<int>();
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                var writtenSize = file.SaveFileData(output);
                sizeList.Add((int)writtenSize);
            }

            output.Position = 0x4;
            bw.WriteMultiple(sizeList);
        }
    }
}
