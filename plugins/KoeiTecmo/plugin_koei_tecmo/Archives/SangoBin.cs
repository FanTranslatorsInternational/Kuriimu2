using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_koei_tecmo.Archives
{
    class SangoBin
    {
        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read offsets
            var fileCount = br.ReadInt32();
            var offsets = ReadIntegers(br, fileCount);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < fileCount; i++)
            {
                var offset = offsets[i];
                var length = (i + 1 == fileCount ? input.Length : offsets[i + 1]) - offset;

                var fileStream = new SubStream(input, offset, length);
                var fileName = $"{i:00000000}.bin";

                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = fileStream
                }));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var dataOffset = 4 + files.Count * 4;

            // Write files
            var offsets = new List<int>();

            var dataPosition = dataOffset;
            foreach (var file in files)
            {
                // Write file data
                output.Position = dataPosition;
                var writtenSize = file.WriteFileData(output);

                offsets.Add(dataPosition);
                dataPosition += (int)writtenSize;
            }

            // Write offsets
            output.Position = 0;
            bw.Write(files.Count);
            WriteIntegers(offsets, bw);
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
