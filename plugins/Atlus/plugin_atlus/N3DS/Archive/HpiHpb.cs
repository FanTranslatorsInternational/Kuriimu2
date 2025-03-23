using System.Text;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Extensions;

namespace plugin_atlus.N3DS.Archive
{
    public class HpiHpb
    {
        private static readonly int HeaderSize = 0x18;
        private static readonly int HashEntrySize = 4;
        private static readonly int FileEntrySize = 0x10;
        private static readonly Encoding encoding = Encoding.GetEncoding("SJIS");

        private const int HashSlotCount_ = 0x1000;

        public List<HpiHpbArchiveFile> Load(Stream hpiStream, Stream hpbStream)
        {
            var typeReader = new BinaryTypeReader();

            using var hpiBr = new BinaryReaderX(hpiStream, encoding);

            // Read header
            var header = typeReader.Read<HpiHeader>(hpiBr);

            // Read hashes
            for (int i = 0; i < header.hashCount; i++)
            {
                typeReader.Read<HpiHashEntry>(hpiBr);
            }

            // Read entries
            List<HpiFileEntry> entries = new List<HpiFileEntry>();
            for (int i = 0; i < header.entryCount; i++)
            {
                var entry = typeReader.Read<HpiFileEntry>(hpiBr);
                if (entry != null)
                {
                    entries.Add(entry);
                }
            }

            // Prepare string table
            var stringStream = new SubStream(hpiStream, hpiStream.Position, hpiStream.Length - hpiStream.Position);
            using var stringBr = new BinaryReaderX(stringStream, encoding);

            var t = entries.Select(x => (x.offset, x.offset + x.compSize)).OrderByDescending(x => x.offset).ToArray();

            // Add files
            List<HpiHpbArchiveFile> result = new List<HpiHpbArchiveFile>();

            foreach (var entry in entries)
            {
                stringStream.Position = entry.stringOffset;
                result.Add(
                    new HpiHpbArchiveFile(
                        new ArchiveFileInfo
                        {
                            FilePath = stringBr.ReadNullTerminatedString(),
                            FileData = new SubStream(hpbStream, entry.offset >= hpbStream.Length ? 0 : entry.offset, entry.compSize)
                        },
                        entries[0]
                    )
                );
            }            

            return result;
        }

        public void Save(Stream hpiStream, Stream hpbStream, List<HpiHpbArchiveFile> files)
        {
            var sjis = Encoding.GetEncoding("SJIS");
            var hash = new Kryptography.Checksum.Simple(0x25);

            var typeWriter = new BinaryTypeWriter();
            using var hpiBw = new BinaryWriterX(hpiStream);

            // Calculate offsets
            var fileOffset = 0;
            var hashOffset = HeaderSize;
            var entryOffset = hashOffset + HashSlotCount_ * HashEntrySize;
            var stringOffset = entryOffset + files.Count * FileEntrySize;

            // Group files
            var fileLookup = files.ToLookup(x => hash.ComputeValue(x.FilePath.ToRelative().FullName, sjis) % HashSlotCount_);

            // Write files and strings
            hpiStream.Position = stringOffset;
            hpbStream.Position = fileOffset;

            foreach (var file in files.OrderBy(x => x.FilePath, new SlashFirstStringComparer()))
            {
                fileOffset = (int)hpbStream.Position;
                var nameOffset = (int)hpiStream.Position;

                var writtenSize = file.WriteFileData(hpbStream, false);
                hpiBw.WriteString(file.FilePath.ToRelative().FullName, sjis, false);

                file.Entry.offset = fileOffset;
                file.Entry.stringOffset = nameOffset - stringOffset;
                file.Entry.compSize = (int)writtenSize;
                file.Entry.decompSize = file.UsesCompression ? (int)file.FileSize : 0;
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

                foreach (var file in fileLookup[i])
                    typeWriter.Write(file.Entry, hpiBw);
            }

            // Write hash entries
            hpiStream.Position = hashOffset;
            foreach (var h in hashes)
            {
                typeWriter.Write(h, hpiBw);
            }

            // Write header
            hpiStream.Position = 0;
            typeWriter.Write(new HpiHeader
            {
                hashCount = (short)hashes.Count,
                entryCount = files.Count
            }, hpiBw);
        }
    }
}
