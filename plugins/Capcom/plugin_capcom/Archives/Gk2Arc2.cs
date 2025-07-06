using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_capcom.Archives
{
    class Gk2Arc2
    {
        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read first offset
            var firstOffset = br.ReadInt32();
            var fileCount = firstOffset / 4;

            // Read all offsets
            input.Position = 0;
            var offsets = ReadIntegers(br, fileCount);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < fileCount; i++)
            {
                var offset = offsets[i];
                var size = i + 1 >= fileCount ? input.Length - offset : offsets[i + 1] - offset;

                var subStream = new SubStream(input, offset, size);
                var fileName = $"{i:00000000}{Gk2Arc2Support.DetermineExtension(subStream)}";

                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = subStream
                }));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            // Calculate offset
            var fileOffset = files.Count * 4;

            // Write files
            var offsets = new List<int>();

            var filePosition = fileOffset;
            foreach (var file in files)
            {
                offsets.Add(filePosition);

                output.Position = filePosition;
                var writtenSize = file.WriteFileData(output);

                filePosition += (int)((writtenSize + 3) & ~3);
            }

            // Write offsets
            using var bw = new BinaryWriterX(output);

            output.Position = 0;
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
