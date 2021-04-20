using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;
using Kryptography.Hash;

namespace Atlus.Archives
{
    class HpiHpb
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(HpiHeader));
        private static readonly int HashEntrySize = Tools.MeasureType(typeof(HpiHashEntry));
        private static readonly int FileEntrySize = Tools.MeasureType(typeof(HpiFileEntry));

        private const int HashSlotCount_ = 0x1000;

        public IList<IArchiveFileInfo> Load(Stream hpiStream, Stream hpbStream)
        {
            using var hpiBr = new BinaryReaderX(hpiStream);

            // Read header
            var header = hpiBr.ReadType<HpiHeader>();

            // Read hashes
            hpiBr.ReadMultiple<HpiHashEntry>(header.hashCount);

            // Read entries
            var entries = hpiBr.ReadMultiple<HpiFileEntry>(header.entryCount);

            // Prepare string table
            var stringStream = new SubStream(hpiStream, hpiStream.Position, hpiStream.Length - hpiStream.Position);
            using var stringBr = new BinaryReaderX(stringStream);

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var entry in entries)
            {
                var subStream = new SubStream(hpbStream, entry.offset, entry.compSize);

                stringStream.Position = entry.stringOffset;
                var name = stringBr.ReadCStringSJIS();

                result.Add(CreateAfi(subStream, name, entry));
            }

            return result;
        }

        public void Save(Stream hpiStream, Stream hpbStream, IList<IArchiveFileInfo> files)
        {
            var sjis = Encoding.GetEncoding("SJIS");
            var hash = new SimpleHash(0x25);

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
            foreach (var file in files.Cast<HpiHpbArchiveFileInfo>().OrderBy(x => x.FilePath, new SlashFirstStringComparer()))
            {
                fileOffset = (int)hpbStream.Position;
                var nameOffset = (int)hpiStream.Position;

                var writtenSize = file.SaveFileData(hpbStream);
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

                foreach (var file in fileLookup[i].Cast<HpiHpbArchiveFileInfo>())
                    hpiBw.WriteType(file.Entry);
            }

            // Write hash entries
            hpiStream.Position = hashOffset;
            hpiBw.WriteMultiple(hashes);

            // Write header
            hpiStream.Position = 0;
            hpiBw.WriteType(new HpiHeader
            {
                hashCount = (short)hashes.Count,
                entryCount = files.Count
            });
        }

        private ArchiveFileInfo CreateAfi(Stream file, string name, HpiFileEntry entry)
        {
            var magic = HpiHpbSupport.PeekString(file, 4);

            if (magic != "ACMP")
                return new HpiHpbArchiveFileInfo(file, name, entry);

            var compressedStream = new SubStream(file, 0x20, file.Length - 0x20);
            return new HpiHpbArchiveFileInfo(compressedStream, name, entry, Kompression.Implementations.Compressions.Nintendo.BackwardLz77, entry.decompSize);
        }
    }
}
