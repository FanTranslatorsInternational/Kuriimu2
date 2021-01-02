using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_circus.Archives
{
    class Main
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(MainHeader));

        private MainHeader _header;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadType<MainHeader>();

            // Calculate file count
            var firstOffset = br.ReadInt32();
            var fileCount = (firstOffset - HeaderSize) / 4 - 1;

            // Read offsets
            input.Position = HeaderSize;
            var offsets = br.ReadMultiple<int>(fileCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < fileCount; i++)
            {
                var offset = offsets[i];
                var size = (i + 1 >= fileCount ? input.Length : offsets[i + 1]) - offset;

                var subStream = new SubStream(input, offset, size);
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
            var fileOffset = entryOffset + (files.Count + 1) * 4;

            // Write files
            var offsets = new List<int>();

            var filePosition = fileOffset;
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                output.Position = filePosition;
                var writtenSize = file.SaveFileData(output);

                offsets.Add(filePosition);

                filePosition += (int)writtenSize;
            }
            offsets.Add((int)output.Length);

            // Write offsets
            output.Position = entryOffset;
            bw.WriteMultiple(offsets);

            // Write header
            output.Position = 0;
            _header.fileSize = (int)output.Length;
            bw.WriteType(_header);
        }
    }
}
