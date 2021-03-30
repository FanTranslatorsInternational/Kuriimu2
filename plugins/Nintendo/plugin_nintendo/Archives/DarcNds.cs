using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BCnEncoder.Shared;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_nintendo.Archives
{
    class DarcNds
    {
        private static int HeaderSize = Tools.MeasureType(typeof(DarcNdsHeader));

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = br.ReadType<DarcNdsHeader>();

            // Read offsets
            var offsets = br.ReadMultiple<int>(header.fileCount);

            // Add files
            var result = new List<IArchiveFileInfo>();

            var baseOffset = 8;
            for (var i = 0; i < header.fileCount; i++)
            {
                input.Position = offsets[i] + baseOffset;
                var fileSize = br.ReadInt32();

                var fileStream = new SubStream(input, input.Position, fileSize);
                var fileName = $"{i:00000000}.bin";

                result.Add(new ArchiveFileInfo(fileStream, fileName));

                baseOffset += 4;
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var offsetsOffset = HeaderSize;
            var dataOffset = offsetsOffset + files.Count * 4;

            // Write files
            var offsets = new List<int>(files.Count);

            var baseOffset = HeaderSize;
            var fileOffset = dataOffset;
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                output.Position = fileOffset;
                bw.Write((int)file.FileSize);
                file.SaveFileData(output);

                offsets.Add(fileOffset - baseOffset);

                fileOffset += (int)((file.FileSize + 4 + 3) & ~3);
                baseOffset += 4;
            }

            // Write offsets
            output.Position = offsetsOffset;
            bw.WriteMultiple(offsets);

            // Write header
            var header = new DarcNdsHeader
            {
                fileCount = files.Count
            };

            output.Position = 0;
            bw.WriteType(header);
        }
    }
}
