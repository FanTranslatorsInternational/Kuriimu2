using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_circus.Archives
{
    class Main
    {
        private static readonly int HeaderSize = 0x10;

        private MainHeader _header;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = ReadHeader(br);

            // Calculate file count
            var firstOffset = br.ReadInt32();
            var fileCount = (firstOffset - HeaderSize) / 4 - 1;

            // Read offsets
            input.Position = HeaderSize;
            var offsets = ReadIntegers(br, fileCount);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < fileCount; i++)
            {
                var offset = offsets[i];
                var size = (i + 1 >= fileCount ? input.Length : offsets[i + 1]) - offset;

                var subStream = new SubStream(input, offset, size);
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
            var entryOffset = HeaderSize;
            var fileOffset = entryOffset + (files.Count + 1) * 4;

            // Write files
            var offsets = new List<int>();

            var filePosition = fileOffset;
            foreach (var file in files)
            {
                output.Position = filePosition;
                var writtenSize = file.WriteFileData(output);

                offsets.Add(filePosition);

                filePosition += (int)writtenSize;
            }
            offsets.Add((int)output.Length);

            // Write offsets
            output.Position = entryOffset;
            WriteIntegers(offsets, bw);

            // Write header
            output.Position = 0;
            _header.fileSize = (int)output.Length;
            WriteHeader(_header, bw);
        }

        private MainHeader ReadHeader(BinaryReaderX reader)
        {
            return new MainHeader
            {
                magic = reader.ReadString(4),
                unk1 = reader.ReadInt32(),
                fileSize = reader.ReadInt32(),
                unk2 = reader.ReadInt32()
            };
        }

        private int[] ReadIntegers(BinaryReaderX reader, int count)
        {
            var result = new int[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadInt32();

            return result;
        }

        private void WriteHeader(MainHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.unk1);
            writer.Write(header.fileSize);
            writer.Write(header.unk2);
        }

        private void WriteIntegers(IList<int> entries, BinaryWriterX writer)
        {
            foreach (int entry in entries)
                writer.Write(entry);
        }
    }
}
