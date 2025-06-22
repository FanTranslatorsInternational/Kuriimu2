using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_nintendo.Archives
{
    class DarcNds
    {
        private const int HeaderSize_ = 0x8;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = ReadHeader(br);

            // Read offsets
            var offsets = ReadIntegers(br, header.fileCount);

            // Add files
            var result = new List<IArchiveFile>();

            var baseOffset = 8;
            for (var i = 0; i < header.fileCount; i++)
            {
                input.Position = offsets[i] + baseOffset;
                var fileSize = br.ReadInt32();

                var fileStream = new SubStream(input, input.Position, fileSize);
                var fileName = $"{i:00000000}.bin";

                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = fileStream
                }));

                baseOffset += 4;
            }

            return result;
        }

        public void Save(Stream output, List<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var offsetsOffset = HeaderSize_;
            var dataOffset = offsetsOffset + files.Count * 4;

            // Write files
            var offsets = new List<int>(files.Count);

            var baseOffset = HeaderSize_;
            var fileOffset = dataOffset;
            foreach (var file in files)
            {
                output.Position = fileOffset;
                bw.Write((int)file.FileSize);
                file.WriteFileData(output);

                offsets.Add(fileOffset - baseOffset);

                fileOffset += (int)((file.FileSize + 4 + 3) & ~3);
                baseOffset += 4;
            }

            // Write offsets
            output.Position = offsetsOffset;
            WriteIntegers(offsets, bw);

            // Write header
            var header = new DarcNdsHeader
            {
                magic = "DARC",
                fileCount = files.Count
            };

            output.Position = 0;
            WriteHeader(header, bw);
        }

        private DarcNdsHeader ReadHeader(BinaryReaderX reader)
        {
            return new DarcNdsHeader
            {
                magic = reader.ReadString(4),
                fileCount = reader.ReadInt32()
            };
        }

        private int[] ReadIntegers(BinaryReaderX reader, int count)
        {
            var result = new int[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadInt32();

            return result;
        }

        private void WriteHeader(DarcNdsHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic,writeNullTerminator:false);
            writer.Write(header.fileCount);
        }

        private void WriteIntegers(IList<int> entries, BinaryWriterX writer)
        {
            foreach (int entry in entries)
                writer.Write(entry);
        }
    }
}
