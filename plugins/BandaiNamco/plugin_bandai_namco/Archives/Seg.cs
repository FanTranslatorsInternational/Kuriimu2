using Komponent.IO;
using Komponent.Streams;
using Kompression;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_bandai_namco.Archives
{
    class Seg
    {
        public List<IArchiveFile> Load(Stream segStream, Stream binStream, Stream sizeStream)
        {
            using var segBr = new BinaryReaderX(segStream);

            // Read offsets
            var offsets = ReadIntegers(segBr, (int)(segStream.Length / 4));

            // Read decompressed sizes
            var decompressedSizes = Array.Empty<int>();
            if (sizeStream != null)
            {
                using var sizeBr = new BinaryReaderX(sizeStream);
                decompressedSizes = ReadIntegers(sizeBr, (int)(sizeStream.Length / 4)).ToArray();
            }

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < offsets.Length - 1; i++)
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

        public void Save(Stream segStream, Stream binStream, Stream sizeStream, IList<IArchiveFile> files)
        {
            using var binBw = new BinaryWriterX(binStream);
            using var segBw = new BinaryWriterX(segStream);

            // Write files
            var offsets = new List<int>();
            var decompressedSizes = new List<int>();

            foreach (var file in files)
            {
                offsets.Add((int)binStream.Position);
                decompressedSizes.Add((int)file.FileSize);

                file.WriteFileData(binStream);
                binBw.WriteAlignment(0x10);
            }

            // Write offsets
            WriteIntegers(offsets, segBw);
            segBw.Write((int)binStream.Length);

            // Write decompressed sizes
            if (sizeStream != null)
            {
                using var sizeBw = new BinaryWriterX(sizeStream);

                WriteIntegers(decompressedSizes, sizeBw);
                sizeBw.Write(0);
            }
        }

        private IArchiveFile CreateAfi(Stream file, string fileName, int decompressedSize)
        {
            if (decompressedSize > 0)
                return new ArchiveFile(new CompressedArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = file,
                    Compression = Compressions.LzssVlc.Build(),
                    DecompressedSize = decompressedSize
                });

            return new ArchiveFile(new ArchiveFileInfo
            {
                FilePath = fileName,
                FileData = file
            });
        }

        private int[] ReadIntegers(BinaryReaderX reader, int count)
        {
            var result = new int[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadInt32();

            return result;
        }

        private void WriteIntegers(IList<int> entries, BinaryWriterX writer)
        {
            foreach (int entry in entries)
                writer.Write(entry);
        }
    }
}
