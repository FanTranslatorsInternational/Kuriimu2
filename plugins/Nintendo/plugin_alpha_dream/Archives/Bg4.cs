using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kompression.Implementations;
using Kontract.Extensions;
using Kontract.Models.Archive;
using Kryptography.Hash;

namespace plugin_alpha_dream.Archives
{
    class Bg4
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(Bg4Header));
        private static readonly int EntrySize = Tools.MeasureType(typeof(Bg4Entry));

        private const int HashSeed_ = 0x1F;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = br.ReadType<Bg4Header>();

            // Read entries
            var entries = br.ReadMultiple<Bg4Entry>(header.fileEntryCount);

            // Prepare string stream
            var stringStream = new SubStream(input, br.BaseStream.Position, header.metaSecSize - br.BaseStream.Position);
            using var stringBr = new BinaryReaderX(stringStream);

            // Add files
            var result = new List<IArchiveFileInfo>();

            foreach (var entry in entries.Where(x => !x.IsInvalid))
            {
                var subStream = new SubStream(input, entry.FileOffset, entry.FileSize);

                stringBr.BaseStream.Position = entry.nameOffset;
                var fileName = stringBr.ReadCStringASCII();

                result.Add(CreateAfi(subStream, fileName, entry));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            var hash = new SimpleHash(HashSeed_);
            using var bw = new BinaryWriterX(output);

            // Create string dictionary
            var stringPosition = 0;
            var stringDictionary = new Dictionary<string, int>();

            foreach (var distinctString in files.Select(x => x.FilePath.ToRelative().FullName).Distinct())
            {
                stringDictionary[distinctString] = stringPosition;
                stringPosition += Encoding.ASCII.GetByteCount(distinctString) + 1;
            }

            // Calculate offsets
            var entryOffset = HeaderSize;
            var stringOffset = entryOffset + files.Count * EntrySize;
            var fileOffset = (stringOffset + stringPosition + 3) & ~3;
            var filePosition = fileOffset;

            // Write files
            var entries = new List<Bg4Entry>();
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                output.Position = filePosition;
                var writtenSize = file.SaveFileData(output);

                // Create entry
                var fileName = file.FilePath.ToRelative().FullName;
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
                bw.WriteString(distinctString, Encoding.ASCII, false);
            bw.WriteAlignment(4, 0xFF);

            // Write entries
            output.Position = entryOffset;
            bw.WriteMultiple(entries);

            // Write header
            output.Position = 0;
            bw.WriteType(new Bg4Header
            {
                fileEntryCount = (short)files.Count,
                metaSecSize = fileOffset,
                fileEntryCountMultiplier = 1,
                fileEntryCountDerived = (short)files.Count
            });
        }

        private IArchiveFileInfo CreateAfi(Stream fileStream, string fileName, Bg4Entry entry)
        {
            if (!entry.IsCompressed)
                return new ArchiveFileInfo(fileStream, fileName);

            return new ArchiveFileInfo(fileStream, fileName, Compressions.Nintendo.BackwardLz77, Bg4Support.PeekDecompressedSize(fileStream));
        }

        private string ReverseString(string value)
        {
            return value.Reverse().Aggregate("", (a, b) => a + b);
        }
    }
}
