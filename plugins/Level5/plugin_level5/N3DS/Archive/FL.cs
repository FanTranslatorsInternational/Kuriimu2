using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using plugin_level5.Common.Compression;

namespace plugin_level5.N3DS.Archive
{
    public class FL
    {
        private int _fileCount;
        private HashSet<int> _unusedIndexes = new();

        public List<FLArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read file count
            _fileCount = br.ReadInt32();
            input.Position += 4;

            // Read offsets
            var offsets = ReadIntegers(br, _fileCount);

            // Read uncompressed sizes
            var uncompSizes = ReadIntegers(br, _fileCount);

            // Read compressed sizes
            var compSizes = ReadIntegers(br, _fileCount);

            // Read compression flags
            var flags = ReadBooleans(br, _fileCount);

            // Add files
            var index = 0;

            var result = new List<FLArchiveFile>();
            for (var i = 0; i < _fileCount; i++)
            {
                int fileSize = flags[i] ? compSizes[i] : uncompSizes[i];

                // HINT: The archive can have the same offset multiple times in sequence.
                // Only the last of those offsets is actually used, which would also mean, that the other offsets are left over from non-existent files
                // Keep track of those repeating offsets for save integrity
                if (fileSize == 0)
                {
                    _unusedIndexes.Add(i);
                    continue;
                }

                var fileStream = new SubStream(input, offsets[i], fileSize);
                var fileName = $"{index++:00000000}{FLSupport.DetermineExtension(fileStream)}";

                ArchiveFileInfo fileInfo;
                if (flags[i])
                    fileInfo = new CompressedArchiveFileInfo
                    {
                        FileData = fileStream,
                        FilePath = fileName,
                        Compression = NintendoCompressor.GetConfiguration(NintendoCompressor.PeekCompressionMethod(fileStream)),
                        DecompressedSize = uncompSizes[i]
                    };
                else
                    fileInfo = new ArchiveFileInfo
                    {
                        FileData = fileStream,
                        FilePath = fileName
                    };

                result.Add(new FLArchiveFile(fileInfo, i));
            }

            return result;
        }

        public void Save(Stream output, List<FLArchiveFile> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var offsetsOffset = 8;
            var uncompOffset = offsetsOffset + _fileCount * 4;
            var compOffset = uncompOffset + _fileCount * 4;
            var compFlagsOffset = compOffset + _fileCount * 4;
            var dataOffset = (compFlagsOffset + _fileCount + 0x1F) & ~0x1F;

            // Write files
            var offsets = new int[_fileCount];
            var uncompSizes = new int[_fileCount];
            var compSizes = new int[_fileCount];
            var compFlags = new bool[_fileCount];

            var dataPosition = dataOffset;
            var lastIndex = 0;

            for (var i = 0; i < _fileCount; i++)
            {
                if (_unusedIndexes.Contains(i))
                    continue;

                // Write file data
                output.Position = dataPosition;
                var writtenSize = files[i].WriteFileData(output, true);
                bw.WriteAlignment(0x20);

                while (lastIndex <= files[i].Index)
                {
                    offsets[lastIndex] = dataPosition;
                    uncompSizes[lastIndex] = (int)files[i].FileSize;
                    compSizes[lastIndex] = (int)writtenSize;
                    compFlags[lastIndex] = files[i].UsesCompression;

                    lastIndex++;
                }

                dataPosition = (int)output.Position;
            }

            // Write tables
            output.Position = offsetsOffset;
            WriteIntegers(offsets, bw);
            WriteIntegers(uncompSizes, bw);
            WriteIntegers(compSizes, bw);
            WriteBooleans(compFlags, bw);

            // Write header
            output.Position = 0;
            bw.Write(_fileCount);
        }

        private int[] ReadIntegers(BinaryReaderX reader, int count)
        {
            var result = new int[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadInt32();

            return result;
        }

        private bool[] ReadBooleans(BinaryReaderX reader, int count)
        {
            var result = new bool[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadBoolean();

            return result;
        }

        private void WriteIntegers(int[] entries, BinaryWriterX writer)
        {
            foreach (int entry in entries)
                writer.Write(entry);
        }

        private void WriteBooleans(bool[] entries, BinaryWriterX writer)
        {
            foreach (bool entry in entries)
                writer.Write(entry);
        }
    }
}
