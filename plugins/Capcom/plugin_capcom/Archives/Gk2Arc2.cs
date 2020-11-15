using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_capcom.Archives
{
    class Gk2Arc2
    {
        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read first offset
            var firstOffset = br.ReadInt32();
            var fileCount = firstOffset / 4;

            // Read all offsets
            input.Position = 0;
            var offsets = br.ReadMultiple<int>(fileCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < fileCount; i++)
            {
                var offset = offsets[i];
                var size = i + 1 >= fileCount ? input.Length - offset : offsets[i + 1] - offset;

                var subStream = new SubStream(input, offset, size);
                var fileName = $"{i:00000000}{Gk2Arc2Support.DetermineExtension(subStream)}";

                result.Add(new ArchiveFileInfo(subStream, fileName));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            // Calculate offset
            var fileOffset = files.Count * 4;

            // Write files
            var offsets = new List<int>();

            var filePosition = fileOffset;
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                offsets.Add(filePosition);

                output.Position = filePosition;
                var writtenSize = file.SaveFileData(output);

                filePosition += (int)((writtenSize + 3) & ~3);
            }

            // Write offsets
            using var bw = new BinaryWriterX(output);

            output.Position = 0;
            bw.WriteMultiple(offsets);
        }
    }
}
