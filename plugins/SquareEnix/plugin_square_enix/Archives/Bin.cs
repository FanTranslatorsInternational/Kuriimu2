using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace plugin_square_enix.Archives
{
    class Bin
    {
        private const int HeaderSize_ = 0x20;
        private const int BinTableOffset_ = 0x28;
        public IList<IArchiveFileInfo> Load(Stream input)
        {
            var _entries = new List<IArchiveFileInfo>();
            var reader = new BinaryReaderX(input);

            var fileCountEntry = reader.ReadType<BinTableEntry>();
            reader.BaseStream.Position = fileCountEntry.offset + 0x20;
            var fileCount = reader.ReadInt32();
            reader.BaseStream.Position = BinTableOffset_;
            var binFiles = reader.ReadMultiple<BinTableEntry>(fileCount);
            for (int i = 0; i < fileCount; i++)
            {
                using (var sub = new SubStream(input, binFiles[i].offset + HeaderSize_, binFiles[i].fileSize))
                    _entries.Add(new ArchiveFileInfo(sub, $"file{i}"));
            }
            return _entries;
        }
        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output);
            var unAlignedFileOffset = HeaderSize_ + ((files.Count * 0x08) + 0x08);
            var AlignedFileOffset = AlignBy20(unAlignedFileOffset) + 0x20;

            // First file entry describes count and total data file sizes
            bw.BaseStream.Position = AlignedFileOffset;
            var totalDataFileSize = files.Sum(x => AlignBy20((int)x.FileSize)) + 0x20;
            bw.Write(files.Count);
            bw.Write(totalDataFileSize);
            bw.WriteAlignment(0x20);

            // Write files
            var entries = new List<BinTableEntry>();
            foreach (var file in files.Cast<ArchiveFileInfo>())
            {
                var currentOffset = (int)bw.BaseStream.Position;
                var binEntrySize = (int)file.SaveFileData(output);
                bw.WriteAlignment(0x20);
                entries.Add(new BinTableEntry
                {
                    offset = currentOffset - HeaderSize_,
                    fileSize = binEntrySize
                });
            }

            // Write Bin Table
            bw.BaseStream.Position = HeaderSize_;
            bw.Write(AlignedFileOffset - 0x20);
            bw.Write(0x20);
            bw.WriteMultiple(entries);

            // Write Header
            bw.BaseStream.Position = 0;
            bw.WriteString("pack", Encoding.ASCII, false, false);
            // Number of 8 byte units in file info table, with the alignment included
            bw.Write((AlignedFileOffset - 0x20) / 0x08);
            bw.Write(totalDataFileSize);
        }

        private int AlignBy20(int numberToAlign)
        {
            return (numberToAlign + 0x1F) & ~0x1F;
        }
    }
}
