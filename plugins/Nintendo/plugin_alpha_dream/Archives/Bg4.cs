using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Kompression;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;
using Kryptography.Checksum;

namespace plugin_alpha_dream.Archives
{
    class Bg4
    {
        private const int HeaderSize_ = 0x10;
        private const int EntrySize_ = 0x1E;

        private const int HashSeed_ = 0x1F;

        public List<IArchiveFile> Load(Stream input)
        {
            var typeReader = new BinaryTypeReader();
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = typeReader.Read<Bg4Header>(br);

            // Read entries
            var entries = typeReader.ReadMany<Bg4Entry>(br, header.fileEntryCount);

            // Prepare string stream
            var stringStream = new SubStream(input, br.BaseStream.Position, header.metaSecSize - br.BaseStream.Position);
            using var stringBr = new BinaryReaderX(stringStream);

            // Add files
            var result = new List<IArchiveFile>();

            foreach (Bg4Entry entry in entries.Where(x => !x.IsInvalid))
            {
                var subStream = new SubStream(input, entry.FileOffset, entry.FileSize);

                stringBr.BaseStream.Position = entry.nameOffset;
                string fileName = stringBr.ReadNullTerminatedString();

                result.Add(CreateAfi(subStream, fileName, entry));
            }

            return result;
        }

        public void Save(Stream output, List<IArchiveFile> files)
        {
            var hash = new Simple(HashSeed_);

            var typeWriter = new BinaryTypeWriter();
            using var bw = new BinaryWriterX(output);

            // Create string dictionary
            var stringPosition = 0;
            var stringDictionary = new Dictionary<string, int>();

            foreach (var distinctString in files.Select(x => x.FilePath.FullName).Distinct())
            {
                stringDictionary[distinctString] = stringPosition;
                stringPosition += Encoding.ASCII.GetByteCount(distinctString) + 1;
            }

            // Calculate offsets
            var entryOffset = HeaderSize_;
            var stringOffset = entryOffset + files.Count * EntrySize_;
            var fileOffset = (stringOffset + stringPosition + 3) & ~3;
            var filePosition = fileOffset;

            // Write files
            var entries = new List<Bg4Entry>();
            foreach (var file in files)
            {
                output.Position = filePosition;
                var writtenSize = file.WriteFileData(output);

                // Create entry
                var fileName = file.FilePath.FullName;
                entries.Add(new Bg4Entry
                {
                    FileOffset = filePosition,
                    FileSize = (int)writtenSize,
                    IsCompressed = file.UsesCompression,

                    nameOffset = (short)stringDictionary[fileName],
                    nameHash = hash.ComputeValue(ReverseString(fileName))
                });

                filePosition += (int)writtenSize;
            }

            // Write strings
            output.Position = stringOffset;
            foreach (var distinctString in stringDictionary.Keys)
                bw.WriteString(distinctString, Encoding.ASCII);
            bw.WriteAlignment(4, 0xFF);

            // Write entries
            output.Position = entryOffset;
            typeWriter.WriteMany(entries, bw);

            // Write header
            output.Position = 0;
            typeWriter.Write(new Bg4Header
            {
                fileEntryCount = (short)files.Count,
                metaSecSize = fileOffset,
                fileEntryCountMultiplier = 1,
                fileEntryCountDerived = (short)files.Count
            }, bw);
        }

        private IArchiveFile CreateAfi(Stream fileStream, string fileName, Bg4Entry entry)
        {
            if (!entry.IsCompressed)
                return new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = fileStream
                });

            return new ArchiveFile(new CompressedArchiveFileInfo
            {
                FilePath = fileName,
                FileData = fileStream,
                Compression = Compressions.Nintendo.BackwardLz77.Build(),
                DecompressedSize = (int)Bg4Support.PeekDecompressedSize(fileStream)
            });
        }

        private string ReverseString(string value)
        {
            return value.Reverse().Aggregate("", (a, b) => a + b);
        }
    }
}
