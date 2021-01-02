using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;

namespace plugin_spike_chunsoft.Archives
{
    class Zdp
    {
        private static readonly int PartitionHeaderSize = Tools.MeasureType(typeof(ZdpPartitionHeader));
        private static readonly int HeaderSize = Tools.MeasureType(typeof(ZdpHeader));
        private static readonly int EntrySize = Tools.MeasureType(typeof(ZdpFileEntry));

        private ZdpPartitionHeader _partitionHeader;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read headers
            _partitionHeader = br.ReadType<ZdpPartitionHeader>();
            var header = br.ReadType<ZdpHeader>();

            // Read entries
            var entries = br.ReadMultiple<ZdpFileEntry>(header.entryCount);
            var nameOffsets = br.ReadMultiple<int>(header.nameOffsetCount);

            // Add files
            var result = new List<IArchiveFileInfo>();
            for (var i = 0; i < header.fileCount; i++)
            {
                var entry = entries[i];
                var nameOffset = nameOffsets[i];

                var subStream = new SubStream(input, entry.offset, entry.size);
                input.Position = nameOffset;
                var fileName = br.ReadCStringASCII();

                result.Add(new ArchiveFileInfo(subStream, fileName));
            }

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var headerOffset = PartitionHeaderSize;
            var entryOffset = headerOffset + HeaderSize;
            var nameOffsetsOffset = entryOffset + files.Count * EntrySize;
            var fileOffset = (nameOffsetsOffset + files.Count * 4 + 0x7F) & ~0x7F;

            // Write files
            var entries = new List<ZdpFileEntry>();

            var filePosition = fileOffset;
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                output.Position = filePosition;
                var writtenSize = file.SaveFileData(output);

                entries.Add(new ZdpFileEntry
                {
                    offset = filePosition,
                    size = (int)writtenSize
                });

                filePosition += (int) writtenSize;
            }

            // Write strings
            var stringOffset = (int)output.Length;

            var stringPosition = stringOffset;
            var nameOffsets = new List<int>();
            foreach (var file in files)
            {
                nameOffsets.Add(stringPosition);
                output.Position = stringPosition;
                bw.WriteString(file.FilePath.ToRelative().GetName(), Encoding.ASCII, false);

                stringPosition = (int)output.Position;
            }

            // Write name offsets
            output.Position = nameOffsetsOffset;
            bw.WriteMultiple(nameOffsets);

            // Write entries
            output.Position = entryOffset;
            bw.WriteMultiple(entries);

            // Write header
            output.Position = headerOffset;
            bw.WriteType(new ZdpHeader
            {
                fileCount = files.Count,
                entryCount = (short)entries.Count,
                nameOffsetCount = (short)nameOffsets.Count,
                stringCount = files.Count,
                entryOffset = entryOffset,
                nameOffsetsOffset = nameOffsetsOffset
            });

            // Write partition header
            output.Position = 0;
            bw.WriteType(_partitionHeader);
        }
    }
}
