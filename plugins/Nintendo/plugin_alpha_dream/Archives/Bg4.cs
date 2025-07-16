using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Kompression;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;
using Kryptography.Checksum;

namespace plugin_alpha_dream.Archives
{
    class Bg4
    {
        private const int HeaderSize_ = 0x10;
        private const int EntrySize_ = 0xE;

        private const int HashSeed_ = 0x1F;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = ReadHeader(br);

            // Read entries
            var entries = ReadEntries(br, header.fileEntryCount);

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
                bw.WriteString(distinctString, Encoding.ASCII);
            bw.WriteAlignment(4, 0xFF);

            // Write entries
            entries = entries.OrderBy(x => x.nameHash).ToList();

            output.Position = entryOffset;
            WriteEntries(entries, bw);

            // Write header
            var header = new Bg4Header
            {
                magic = "BG4\0",
                version = 0x105,
                fileEntryCount = (short)files.Count,
                metaSecSize = fileOffset,
                fileEntryCountMultiplier = 1,
                fileEntryCountDerived = (short)files.Count
            };

            output.Position = 0;
            WriteHeader(header, bw);
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

        private Bg4Header ReadHeader(BinaryReaderX reader)
        {
            return new Bg4Header
            {
                magic = reader.ReadString(4),
                version = reader.ReadInt16(),
                fileEntryCount = reader.ReadInt16(),
                metaSecSize = reader.ReadInt32(),
                fileEntryCountDerived = reader.ReadInt16(),
                fileEntryCountMultiplier = reader.ReadInt16()
            };
        }

        private Bg4Entry[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new Bg4Entry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private Bg4Entry ReadEntry(BinaryReaderX reader)
        {
            return new Bg4Entry
            {
                fileOffset = reader.ReadUInt32(),
                fileSize = reader.ReadUInt32(),
                nameHash = reader.ReadUInt32(),
                nameOffset = reader.ReadInt16()
            };
        }

        private void WriteHeader(Bg4Header header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.version);
            writer.Write(header.fileEntryCount);
            writer.Write(header.metaSecSize);
            writer.Write(header.fileEntryCountDerived);
            writer.Write(header.fileEntryCountMultiplier);
        }

        private void WriteEntries(IList<Bg4Entry> entries, BinaryWriterX writer)
        {
            foreach (Bg4Entry entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(Bg4Entry entry, BinaryWriterX writer)
        {
            writer.Write(entry.fileOffset);
            writer.Write(entry.fileSize);
            writer.Write(entry.nameHash);
            writer.Write(entry.nameOffset);
        }
    }
}
