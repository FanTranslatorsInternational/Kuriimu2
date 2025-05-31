using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;

namespace plugin_atlus.N3DS.Archive
{
    class HpiHpb
    {
        private const int HashSlotCount_ = 0x1000;

        private const int HeaderSize_ = 0x18;
        private const int HashEntrySize_ = 4;
        private const int FileEntrySize_ = 0x10;

        private readonly Encoding _sjis = Encoding.GetEncoding("Shift-JIS");

        public List<IArchiveFile> Load(Stream hpiStream, Stream hpbStream)
        {
            using var reader = new BinaryReaderX(hpiStream, _sjis);

            // Read header
            var header = ReadHeader(reader);

            // Read hashes
            for (var i = 0; i < header.hashCount; i++)
                _ = ReadHashEntry(reader);

            // Read entries
            var entries = new HpiFileEntry[header.entryCount];
            for (var i = 0; i < header.entryCount; i++)
                entries[i] = ReadFileEntry(reader);

            // Prepare string table
            var stringStream = new SubStream(hpiStream, hpiStream.Position, hpiStream.Length - hpiStream.Position);
            using var stringBr = new BinaryReaderX(stringStream);

            // Add files
            var result = new List<IArchiveFile>();
            foreach (HpiFileEntry entry in entries)
            {
                var subStream = new SubStream(hpbStream, entry.offset >= hpbStream.Length ? 0 : entry.offset, entry.compSize);

                stringStream.Position = entry.stringOffset;
                string name = stringBr.ReadNullTerminatedString();
                result.Add(CreateFile(subStream, name, entry));
            }

            return result;
        }

        public void Save(Stream hpiStream, Stream hpbStream, List<IArchiveFile> files)
        {
            var hash = new Kryptography.Checksum.Simple(0x25);

            using var writer = new BinaryWriterX(hpiStream);

            // Calculate offsets
            var fileOffset = 0;
            var hashOffset = HeaderSize_;
            var entryOffset = hashOffset + HashSlotCount_ * HashEntrySize_;
            var stringOffset = entryOffset + files.Count * FileEntrySize_;

            // Group files
            var fileLookup = files.ToLookup(x => hash.ComputeValue(x.FilePath.ToRelative().FullName, _sjis) % HashSlotCount_);

            // Write files and strings
            var entryLookup = new Dictionary<IArchiveFile, HpiFileEntry>();

            hpiStream.Position = stringOffset;
            hpbStream.Position = fileOffset;
            foreach (IArchiveFile file in files.OrderBy(x => x.FilePath, new SlashFirstStringComparer()))
            {
                fileOffset = (int)hpbStream.Position;
                var nameOffset = (int)hpiStream.Position;

                var writtenSize = WriteFile(hpbStream, file);
                writer.WriteString(file.FilePath.ToRelative().FullName, _sjis);

                var entry = new HpiFileEntry
                {
                    offset = fileOffset,
                    stringOffset = nameOffset - stringOffset,
                    compSize = (int)writtenSize,
                    decompSize = file.UsesCompression ? (int)file.FileSize : 0
                };
                entryLookup[file] = entry;
            }

            // Write entries
            var hashes = new List<HpiHashEntry>();

            hpiStream.Position = entryOffset;
            for (uint i = 0, offset = 0; i < HashSlotCount_; i++)
            {
                var hashEntry = new HpiHashEntry
                {
                    entryOffset = (short)offset,
                    entryCount = (short)fileLookup[i].Count()
                };
                hashes.Add(hashEntry);
                offset += (uint)hashEntry.entryCount;

                foreach (IArchiveFile file in fileLookup[i])
                {
                    if (entryLookup.TryGetValue(file, out HpiFileEntry entry))
                        WriteFileEntry(entry, writer);
                }
            }

            // Write hash entries
            hpiStream.Position = hashOffset;
            foreach (HpiHashEntry hashEntry in hashes)
                WriteHashEntry(hashEntry, writer);

            // Write header
            hpiStream.Position = 0;
            WriteHeader(new HpiHeader
            {
                magic = "HPIH",
                headerSize = 0x10,
                hashCount = (short)hashes.Count,
                entryCount = files.Count
            }, writer);
        }

        private HpiHeader ReadHeader(BinaryReaderX reader)
        {
            return new HpiHeader
            {
                magic = reader.ReadString(4),
                zero0 = reader.ReadInt32(),
                headerSize = reader.ReadInt32(),
                zero1 = reader.ReadInt32(),
                zero2 = reader.ReadInt16(),
                hashCount = reader.ReadInt16(),
                entryCount = reader.ReadInt32()
            };
        }

        private HpiHashEntry ReadHashEntry(BinaryReaderX reader)
        {
            return new HpiHashEntry
            {
                entryOffset = reader.ReadInt16(),
                entryCount = reader.ReadInt16(),
            };
        }

        private HpiFileEntry ReadFileEntry(BinaryReaderX reader)
        {
            return new HpiFileEntry
            {
                stringOffset = reader.ReadInt32(),
                offset = reader.ReadInt32(),
                compSize = reader.ReadInt32(),
                decompSize = reader.ReadInt32()
            };
        }

        private void WriteHeader(HpiHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.zero0);
            writer.Write(header.headerSize);
            writer.Write(header.zero1);
            writer.Write(header.zero2);
            writer.Write(header.hashCount);
            writer.Write(header.entryCount);
        }

        private void WriteHashEntry(HpiHashEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.entryOffset);
            writer.Write(entry.entryCount);
        }

        private void WriteFileEntry(HpiFileEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.stringOffset);
            writer.Write(entry.offset);
            writer.Write(entry.compSize);
            writer.Write(entry.decompSize);
        }

        private IArchiveFile CreateFile(Stream file, string name, HpiFileEntry entry)
        {
            string magic = HpiHpbSupport.PeekString(file, 4);
            if (magic != "ACMP")
                return new ArchiveFile(new ArchiveFileInfo { FilePath = name, FileData = file });

            var compressedStream = new SubStream(file, 0x20, file.Length - 0x20);
            return new ArchiveFile(new CompressedArchiveFileInfo
            {
                FilePath = name,
                FileData = compressedStream,
                Compression = Kompression.Compressions.Nintendo.BackwardLz77.Build(),
                DecompressedSize = entry.decompSize
            });
        }

        private long WriteFile(Stream output, IArchiveFile file)
        {
            var position = output.Position;

            var offset = 0;
            if (file.UsesCompression)
                offset = 0x20;

            output.Position += offset;
            var writtenSize = file.WriteFileData(output, file.UsesCompression);

            // Padding
            while (output.Position % 4 != 0)
                output.WriteByte(0);

            if (!file.UsesCompression)
                return writtenSize + offset;

            var bkPos = output.Position;
            using var bw = new BinaryWriterX(output, true);

            output.Position = position;
            bw.WriteString("ACMP", Encoding.ASCII, false, false);
            bw.Write((int)writtenSize);
            bw.Write(0x20);
            bw.Write(0);
            bw.Write((int)file.FileSize);
            bw.Write(0x01234567);
            bw.Write(0x01234567);
            bw.Write(0x01234567);

            output.Position = bkPos;
            return writtenSize + offset;
        }
    }
}
