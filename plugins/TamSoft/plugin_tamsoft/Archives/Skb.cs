using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;

namespace plugin_tamsoft.Archives
{
    class Skb
    {
        private byte[] _header;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = br.ReadBytes(0x18);

            // Read entry count
            br.BaseStream.Position = 0x20;
            var entryCount = br.ReadInt32();

            // Read offsets
            var offsets = ReadIntegers(br, entryCount);

            // Read sizes
            var sizes = ReadIntegers(br, entryCount);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < entryCount; i++)
            {
                var subStream = new SubStream(input, offsets[i], sizes[i]);
                var name = $"{i:00000000}{SkbSupport.DetermineExtension(subStream)}";

                result.Add(new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = name,
                    FileData = subStream
                }));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var entryOffset = 0x20;
            var fileOffset = (entryOffset + 4 + files.Count * 8 + 0x7F) & ~0x7F;

            // Write files
            var offsets = new List<int>();
            var sizes = new List<int>();

            output.Position = fileOffset;
            foreach (var file in files)
            {
                fileOffset = (int)output.Position;
                var writtenSize = file.WriteFileData(output);

                bw.WriteAlignment(0x80);

                offsets.Add(fileOffset);
                sizes.Add((int)writtenSize);
            }

            // Write entries
            output.Position = entryOffset;

            bw.Write(files.Count);
            WriteIntegers(offsets, bw);
            WriteIntegers(sizes, bw);

            // Write header
            output.Position = 0;
            bw.Write(_header);
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
