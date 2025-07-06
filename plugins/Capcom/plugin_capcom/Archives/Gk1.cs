using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_capcom.Archives
{
    class Gk1
    {
        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            br.BaseStream.Position += 8;

            // Read first offset
            var firstOffset = br.ReadInt32();
            br.BaseStream.Position -= 4;

            // Read all offsets
            var offsets = ReadIntegers(br, (firstOffset - 8) / 4);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < offsets.Length; i++)
            {
                br.BaseStream.Position = offsets[i];
                var fileSize = br.ReadInt32();

                var subStream = new SubStream(input, offsets[i] + 4, fileSize);
                var fileName = $"{i:00000000}.bin";

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
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = 8;
            var fileOffset = entryOffset + files.Count * 4;

            // Write files
            var offsets = new List<int>();

            var filePosition = fileOffset;
            foreach (var file in files)
            {
                offsets.Add(filePosition);
                output.Position = filePosition;

                // Write size
                bw.Write((int)file.FileSize);

                // Write file data
                file.WriteFileData(output);

                filePosition += (int)(4 + file.FileSize);
            }

            // Write offsets
            output.Position = entryOffset;
            WriteIntegers(offsets, bw);

            // Write header data
            output.Position = 0;
            bw.Write(fileOffset - 4);
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
