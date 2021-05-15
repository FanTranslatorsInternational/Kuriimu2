using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;

namespace plugin_bandai_namco.Archives
{
    class Seg
    {
        public IList<IArchiveFileInfo> Load(Stream segStream, Stream binStream, Stream sizeStream)
        {
            using var segBr = new BinaryReaderX(segStream);

            // Read offsets
            var offsets = segBr.ReadMultiple<int>((int)(segStream.Length / 4));

            // Read decompressed sizes
            var decompressedSizes = Array.Empty<int>();
            if (sizeStream != null)
            {
                using var sizeBr = new BinaryReaderX(sizeStream);
                decompressedSizes = sizeBr.ReadMultiple<int>((int)(sizeStream.Length / 4)).ToArray();
            }

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < offsets.Count - 1; i++)
            {
                var offset = offsets[i];
                if (offset == binStream.Length)
                    break;

                var size = offsets[i + 1] - offset;

                var subStream = new SubStream(binStream, offset, size);
                var fileName = $"{i:00000000}.bin";

                result.Add(CreateAfi(subStream, fileName, sizeStream != null ? decompressedSizes[i] : -1));
            }

            return result;
        }

        public void Save(Stream segStream, Stream binStream, Stream sizeStream, IList<IArchiveFileInfo> files)
        {
            using var binBw = new BinaryWriterX(binStream);
            using var segBw = new BinaryWriterX(segStream);

            // Write files
            var offsets = new List<int>();
            var decompressedSizes = new List<int>();

            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                offsets.Add((int)binStream.Position);
                decompressedSizes.Add((int)file.FileSize);

                file.SaveFileData(binStream);
                binBw.WriteAlignment();
            }

            // Write offsets
            segBw.WriteMultiple(offsets);
            segBw.Write((int)binStream.Length);

            // Write decompressed sizes
            if (sizeStream != null)
            {
                using var sizeBw = new BinaryWriterX(sizeStream);

                sizeBw.WriteMultiple(decompressedSizes);
                sizeBw.Write(0);
            }
        }

        private IArchiveFileInfo CreateAfi(Stream file, string fileName, int decompressedSize)
        {
            if (decompressedSize > 0)
                return new ArchiveFileInfo(file, fileName, Kompression.Implementations.Compressions.LzssVlc, decompressedSize);

            return new ArchiveFileInfo(file, fileName);
        }
    }
}
