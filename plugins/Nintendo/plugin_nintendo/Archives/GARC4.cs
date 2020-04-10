using System.Collections.Generic;
using System.IO;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_nintendo.Archives
{
    public class GARC4
    {
        private static int _headerSize = Tools.MeasureType(typeof(Garc4Header));
        private static int _fatoHeaderSize = Tools.MeasureType(typeof(GarcFatoHeader));
        private static int _fatbHeaderSize = Tools.MeasureType(typeof(GarcFatbHeader));
        private static int _fatbEntrySize = Tools.MeasureType(typeof(Garc4FatbEntry));
        private static int _fimbHeaderSize = Tools.MeasureType(typeof(GarcFimbHeader));

        private ByteOrder _byteOrder;

        public IReadOnlyList<ArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Select byte order
            br.ByteOrder = ByteOrder.BigEndian;
            br.BaseStream.Position = 0x8;
            _byteOrder = (ByteOrder)br.ReadUInt16();

            br.ByteOrder = _byteOrder;

            // Read header
            br.BaseStream.Position = 0;
            var header = br.ReadType<Garc4Header>();

            // Read Fat Offsets
            var fatoHeader = br.ReadType<GarcFatoHeader>();
            var offsets = br.ReadMultiple<int>(fatoHeader.entryCount);

            // Read FATB
            var fatbHeader = br.ReadType<GarcFatbHeader>();
            var fatbOffset = br.BaseStream.Position;

            var fatbEntries = new Garc4FatbEntry[fatoHeader.entryCount];
            for (var i = 0; i < fatoHeader.entryCount; i++)
            {
                br.BaseStream.Position = fatbOffset + offsets[i];
                fatbEntries[i] = br.ReadType<Garc4FatbEntry>();
            }

            // Read FIMB
            br.ReadType<GarcFimbHeader>();

            // Add files
            var result = new List<ArchiveFileInfo>();
            for (var i = 0; i < fatbEntries.Length; i++)
            {
                var fileStream = new SubStream(input, header.dataOffset + fatbEntries[i].offset, fatbEntries[i].size);

                result.Add(GarcSupport.CreateAfi(fileStream, $"{i:00000000}.bin"));
            }

            return result;
        }

        public void Save(Stream output, IReadOnlyList<ArchiveFileInfo> files)
        {
            var fatOffsetPosition = _headerSize;
            var fatbPosition = fatOffsetPosition + _fatoHeaderSize + files.Count * 4;
            var fimbPosition = fatbPosition + _fatbHeaderSize + files.Count * _fatbEntrySize;
            var dataPosition = fimbPosition + _fimbHeaderSize;

            using var bw = new BinaryWriterX(output, _byteOrder);

            // Write file data
            bw.BaseStream.Position = dataPosition;

            var fileEntries = new List<Garc4FatbEntry>();
            var largestFileSize = 0;
            var fileOffset = 0;
            foreach (var file in files)
            {
                var writtenSize = file.SaveFileData(output, null);
                bw.WriteAlignment(4);

                if (largestFileSize < writtenSize)
                    largestFileSize = (int)writtenSize;

                fileEntries.Add(new Garc4FatbEntry
                {
                    offset = (uint)fileOffset,
                    nextFileOffset = (uint)(bw.BaseStream.Position - dataPosition),
                    size = (uint)writtenSize
                });

                fileOffset = (int)(bw.BaseStream.Position - dataPosition);
            }

            bw.BaseStream.Position = fimbPosition;
            bw.WriteType(new GarcFimbHeader
            {
                dataSize = (uint)(bw.BaseStream.Length - dataPosition)
            });

            // Write file entries
            bw.BaseStream.Position = fatbPosition + _fatbHeaderSize;

            var fatOffsets = new List<uint>();
            var fatbOffset = 0u;
            foreach (var entry in fileEntries)
            {
                bw.WriteType(entry);
                fatOffsets.Add(fatbOffset);

                fatbOffset += (uint)_fatbEntrySize;
            }

            bw.BaseStream.Position = fatbPosition;
            bw.WriteType(new GarcFatbHeader
            {
                sectionSize = _fatbHeaderSize + fileEntries.Count * _fatbEntrySize,
                entryCount = fileEntries.Count
            });

            // Write FAT Offsets
            bw.BaseStream.Position = fatOffsetPosition;
            bw.WriteType(new GarcFatoHeader
            {
                sectionSize = _fatoHeaderSize + fatOffsets.Count * 4,
                entryCount = (short)fatOffsets.Count
            });
            bw.WriteMultiple(fatOffsets);

            // Write GARC Header
            bw.BaseStream.Position = 0;
            bw.WriteType(new Garc4Header
            {
                dataOffset = (uint)dataPosition,
                fileSize = (uint)bw.BaseStream.Length,
                headerSize = (uint)_headerSize,
                largestFileSize = (uint)largestFileSize
            });

            // Write byte order
            bw.BaseStream.Position = 8;
            bw.ByteOrder = ByteOrder.BigEndian;
            bw.Write((ushort)_byteOrder);
        }
    }
}
