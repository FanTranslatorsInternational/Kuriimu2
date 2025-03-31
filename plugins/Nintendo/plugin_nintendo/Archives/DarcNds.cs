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
            var typeReader = new BinaryTypeReader();
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = typeReader.Read<DarcNdsHeader>(br);

            // Read offsets
            var offsets = typeReader.ReadMany<int>(br, header.fileCount);

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
            var typeWriter = new BinaryTypeWriter();
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
            typeWriter.WriteMany(offsets, bw);

            // Write header
            var header = new DarcNdsHeader
            {
                fileCount = files.Count
            };

            output.Position = 0;
            typeWriter.Write(header, bw);
        }
    }
}
