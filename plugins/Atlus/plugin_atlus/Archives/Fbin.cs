using System.Collections.Generic;
using System.IO;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_atlus.Archives
{
    class Fbin
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(FbinHeader));

        public IList<ArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = br.ReadType<FbinHeader>();

            // Read sizes
            var sizes = br.ReadMultiple<int>(header.fileCount);

            // Add files
            var result = new List<ArchiveFileInfo>();

            var fileOffset = header.dataOffset;
            for (var i = 0; i < header.fileCount; i++)
            {
                var subStream = new SubStream(input, fileOffset, sizes[i]);
                var name = $"{i:00000000}{FbinSupport.DetermineExtension(subStream)}";

                result.Add(new ArchiveFileInfo(subStream, name));

                fileOffset += sizes[i];
            }

            return result;
        }

        public void Save(Stream output, IList<ArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var sizeOffset = HeaderSize;
            var fileOffset = (sizeOffset + files.Count * 4 + 0xF) & ~0xF;

            // Write files
            var sizes = new List<int>();

            output.Position = fileOffset;
            foreach (var file in files)
            {
                var writtenSize = file.SaveFileData(output);
                sizes.Add((int)writtenSize);
            }

            // Write sizes
            output.Position = sizeOffset;
            bw.WriteMultiple(sizes);

            // Write header
            output.Position = 0;
            bw.WriteType(new FbinHeader
            {
                fileCount = files.Count,
                dataOffset = fileOffset
            });
        }
    }
}
