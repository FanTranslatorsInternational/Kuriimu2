using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_bandai_namco.Archives
{
    class Bin
    {
        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read data offset
            var dataOffset = br.ReadInt32();

            // Read offsets
            var offsets = new List<int> { dataOffset };
            while (input.Position < dataOffset)
            {
                var offset = br.ReadInt32();
                if (offset == 0)
                    break;

                offsets.Add(offset);
            }

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < offsets.Count - 1; i++)
            {
                var subStream = new SubStream(input, offsets[i], offsets[i + 1] - offsets[i]);
                var fileName = $"{i:00000000}{BinSupport.DetermineExtension(subStream)}";

                result.Add(CreateAfi(subStream, fileName));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var fileOffset = ((files.Count + 1) * 4 + 0x7F) & ~0x7F;

            // Write files
            var filePosition = fileOffset;

            var offsets = new List<int>();
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                output.Position = filePosition;
                var writtenSize = file.SaveFileData(output);

                offsets.Add(filePosition);

                filePosition = (filePosition + (int)writtenSize + 0x7F) & ~0x7F;
            }
            offsets.Add(filePosition);

            // Write offsets
            output.Position = 0;
            bw.WriteMultiple(offsets);
        }

        private IArchiveFileInfo CreateAfi(Stream file, string fileName)
        {
            var buffer = new byte[4];

            file.Position = 0;
            file.Read(buffer, 0, 4);

            if (buffer.SequenceEqual(new byte[] { 0x45, 0x43, 0x44, 0x01 }))
            {
                file.Position = 0xC;
                file.Read(buffer, 0, 4);

                var decompressedSize = BinaryPrimitives.ReadInt32BigEndian(buffer);
                return new ArchiveFileInfo(file, fileName, Kompression.Implementations.Compressions.LzEcd, decompressedSize);
            }

            return new ArchiveFileInfo(file, fileName);
        }
    }
}
