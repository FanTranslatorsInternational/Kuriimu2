using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;
using Kontract.Models.IO;
using Kryptography.Hash;

namespace plugin_nintendo.Archives
{
    public class XBB
    {
        private static int _headerSize = 0x20;
        private static int _entrySize = Tools.MeasureType(typeof(XbbFileEntry));
        private static int _hashEntrySize = Tools.MeasureType(typeof(XbbHashEntry));

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            var header = br.ReadType<XbbHeader>();

            // Read entries
            var entries = br.ReadMultiple<XbbFileEntry>(header.entryCount);

            // Read hash entries
            var hashEntries = br.ReadMultiple<XbbHashEntry>(header.entryCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            foreach (var entry in entries)
            {
                var fileStream = new SubStream(input, entry.offset, entry.size);

                br.BaseStream.Position = entry.nameOffset;
                var name = br.ReadCStringASCII();

                result.Add(new ArchiveFileInfo(fileStream, name));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            var entryPosition = _headerSize;
            var hashEntryPosition = entryPosition + files.Count * _entrySize;
            var namePosition = hashEntryPosition + files.Count * _hashEntrySize;

            using var bw = new BinaryWriterX(output);

            // Write names
            bw.BaseStream.Position = namePosition;

            var nameDictionary = new Dictionary<UPath, int>();
            foreach (var file in files)
            {
                if (!nameDictionary.ContainsKey(file.FilePath))
                    nameDictionary.Add(file.FilePath, (int)bw.BaseStream.Position);

                bw.WriteString(file.FilePath.ToRelative().FullName, Encoding.ASCII, false);
            }

            var dataPosition = (bw.BaseStream.Position + 0x7F) & ~0x7F;

            // Write files
            bw.BaseStream.Position = dataPosition;

            var xbbHash = new XbbHash();
            var fileEntries = new List<XbbFileEntry>();
            var hashEntries = new List<XbbHashEntry>();
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                var offset = bw.BaseStream.Position;
                var writtenSize = file.SaveFileData(bw.BaseStream, null);
                bw.WriteAlignment(0x80);

                var hash = xbbHash.Compute(Encoding.ASCII.GetBytes(file.FilePath.ToRelative().FullName));
                fileEntries.Add(new XbbFileEntry
                {
                    offset = (int)offset,
                    size = (int)writtenSize,
                    nameOffset = nameDictionary[file.FilePath],
                    hash = BinaryPrimitives.ReadUInt32BigEndian(hash)
                });

                hashEntries.Add(new XbbHashEntry
                {
                    hash = BinaryPrimitives.ReadUInt32BigEndian(hash),
                    index = fileEntries.Count - 1
                });
            }

            // Write file entries
            bw.BaseStream.Position = entryPosition;
            bw.WriteMultiple(fileEntries);

            // Write hash entries
            bw.BaseStream.Position = hashEntryPosition;
            bw.WriteMultiple(hashEntries.OrderBy(x=>x.hash));

            // Write header
            bw.BaseStream.Position = 0;
            bw.WriteType(new XbbHeader
            {
                entryCount = files.Count
            });
        }
    }
}
