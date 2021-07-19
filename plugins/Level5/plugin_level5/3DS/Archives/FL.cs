using System.Collections.Generic;
using System.IO;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;
using plugin_level5.Compression;
using System.Linq;

namespace plugin_level5._3DS.Archives
{
    class FL
    {
        private int _fileCount;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read file count
            _fileCount = br.ReadInt32();
            input.Position += 4;

            // Read offsets
            var offsets = br.ReadMultiple<int>(_fileCount);

            // Read uncompressed sizes
            var uncompSizes = br.ReadMultiple<int>(_fileCount);

            // Read compressed sizes
            var compSizes = br.ReadMultiple<int>(_fileCount);

            // Read compression flags
            var flags = br.ReadMultiple<bool>(_fileCount);

            // Add files
            var index = 0;

            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < _fileCount; i++)
            {
                // HINT: The archive can have the same offset multiple times in sequence.
                // Only the last of those offsets is actually used, which would also mean, that the other offsets are left over from non-existent files
                // Keep track of those repeating offsets for save integrity
                if (i + 1 < _fileCount && offsets[i] == offsets[i + 1])
                    continue;

                var fileStream = new SubStream(input, offsets[i], flags[i] ? compSizes[i] : uncompSizes[i]);
                var fileName = $"{index++:00000000}{FLSupport.DetermineExtension(fileStream)}";

                if (flags[i])
                    result.Add(new FLArchiveFileInfo(fileStream, fileName, i, NintendoCompressor.GetConfiguration(NintendoCompressor.PeekCompressionMethod(fileStream)), uncompSizes[i]));
                else
                    result.Add(new FLArchiveFileInfo(fileStream, fileName, i));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
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

            foreach (var file in files.Cast<FLArchiveFileInfo>())
            {
                // Write file data
                output.Position = dataPosition;
                var writtenSize = file.SaveFileData(output);
                bw.WriteAlignment(0x20);

                // Update information
                while (lastIndex <= file.FileId)
                {
                    offsets[lastIndex] = dataPosition;
                    uncompSizes[lastIndex] = (int)file.FileSize;
                    compSizes[lastIndex] = (int)writtenSize;
                    compFlags[lastIndex] = file.UsesCompression;

                    lastIndex++;
                }

                dataPosition = (int)output.Position;
            }

            // Write tables
            output.Position = offsetsOffset;
            bw.WriteMultiple(offsets);
            bw.WriteMultiple(uncompSizes);
            bw.WriteMultiple(compSizes);
            bw.WriteMultiple(compFlags);

            // Write header
            output.Position = 0;
            bw.Write(_fileCount);
        }
    }
}
