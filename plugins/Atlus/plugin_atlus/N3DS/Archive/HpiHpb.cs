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
            var typeReader = new BinaryTypeReader();
            using var reader = new BinaryReaderX(hpiStream, _sjis);

            // Read header
            var header = typeReader.Read<HpiHeader>(reader);

            // Read hashes
            typeReader.ReadMany<HpiHashEntry>(reader, header.hashCount);

            // Read entries
            var entries = typeReader.ReadMany<HpiFileEntry>(reader, header.entryCount);

            // Prepare string table
            var stringStream = new SubStream(hpiStream, hpiStream.Position, hpiStream.Length - hpiStream.Position);
            using var stringBr = new BinaryReaderX(stringStream);

            // Add files
            var result = new List<IArchiveFile>();
            foreach (HpiFileEntry entry in entries)
            {
                var subStream = new SubStream(hpbStream, entry.offset >= hpbStream.Length ? 0 : entry.offset, entry.compSize);

                stringStream.Position = entry.stringOffset;
                var name = stringBr.ReadNullTerminatedString();
                result.Add(CreateFile(subStream, name, entry));
            }

            return result;
        }

        public void Save(Stream hpiStream, Stream hpbStream, List<IArchiveFile> files)
        {
            var hash = new Kryptography.Checksum.Simple(0x25);

            var typeWriter = new BinaryTypeWriter();
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
                    if (entryLookup.TryGetValue(file, out HpiFileEntry? entry))
                        typeWriter.Write(entry, writer);
                }
            }

            // Write hash entries
            hpiStream.Position = hashOffset;
            typeWriter.WriteMany(hashes, writer);

            // Write header
            hpiStream.Position = 0;
            typeWriter.Write(new HpiHeader
            {
                hashCount = (short)hashes.Count,
                entryCount = files.Count
            }, writer);
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
