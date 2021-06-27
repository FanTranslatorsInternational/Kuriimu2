using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_koei_tecmo.Archives
{
    class SangoBin
    {
        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read offsets
            var fileCount = br.ReadInt32();
            var offsets = br.ReadMultiple<int>(fileCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < fileCount; i++)
            {
                var offset = offsets[i];
                var length = (i + 1 == fileCount ? input.Length : offsets[i + 1]) - offset;

                var fileStream = new SubStream(input, offset, length);
                var fileName = $"{i:00000000}.bin";

                result.Add(new ArchiveFileInfo(fileStream, fileName));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw=new BinaryWriterX(output);

            // Calculate offsets
            var dataOffset = 4 + files.Count * 4;

            // Write files
            var offsets=new List<int>();

            var dataPosition = dataOffset;
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                // Write file data
                output.Position = dataPosition;
                var writtenSize=file.SaveFileData(output);

                offsets.Add(dataPosition);
                dataPosition += (int)writtenSize;
            }

            // Write offsets
            output.Position = 0;
            bw.Write(files.Count);
            bw.WriteMultiple(offsets);
        }
    }
}
