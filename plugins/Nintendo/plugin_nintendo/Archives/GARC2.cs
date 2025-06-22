using Komponent.Contract.Enums;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_nintendo.Archives
{
    public class Garc2
    {
        private const int HeaderSize_ = 0x18;
        private const int FatoHeaderSize_ = 0xC;
        private const int FatbHeaderSize_ = 0xC;
        private const int FatbEntrySize_ = 0xC;
        private const int FimbHeaderSize_ = 0xC;

        private ByteOrder _byteOrder;

        public List<IArchiveFile> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Select byte order
            br.ByteOrder = ByteOrder.BigEndian;
            br.BaseStream.Position = 0x8;
            _byteOrder = (ByteOrder)br.ReadUInt16();

            br.ByteOrder = _byteOrder;

            // Read header
            br.BaseStream.Position = 0;
            var header = ReadHeader(br);

            // Read Fat Offsets
            var fatoHeader = ReadFatoHeader(br);
            var offsets = ReadIntegers(br, fatoHeader.entryCount);

            // Read FATB
            var fatbHeader = ReadFatbHeader(br);
            var fatbOffset = br.BaseStream.Position;

            var fatbEntries = new Garc2FatbEntry[fatoHeader.entryCount];
            for (var i = 0; i < fatoHeader.entryCount; i++)
            {
                br.BaseStream.Position = fatbOffset + offsets[i];
                fatbEntries[i] = ReadFatbEntry(br);
            }

            // Read FIMB
            _ = ReadFimbHeader(br);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < fatbEntries.Length; i++)
            {
                var fileStream = new SubStream(input, header.dataOffset + fatbEntries[i].offset, fatbEntries[i].nextFileOffset - fatbEntries[i].offset);

                result.Add(GarcSupport.CreateAfi(fileStream, $"{i:00000000}.bin"));
            }

            return result;
        }

        public void Save(Stream output, List<IArchiveFile> files)
        {
            var fatOffsetPosition = HeaderSize_;
            var fatbPosition = fatOffsetPosition + FatoHeaderSize_ + files.Count * 4;
            var fimbPosition = fatbPosition + FatbHeaderSize_ + files.Count * FatbEntrySize_;
            var dataPosition = fimbPosition + FimbHeaderSize_;

            using var bw = new BinaryWriterX(output, _byteOrder);

            // Write file data
            bw.BaseStream.Position = dataPosition;

            var fileEntries = new List<Garc2FatbEntry>();
            var fileOffset = 0;
            foreach (var file in files)
            {
                _ = file.WriteFileData(output);
                bw.WriteAlignment(4);

                fileEntries.Add(new Garc2FatbEntry
                {
                    offset = (uint)fileOffset,
                    nextFileOffset = (uint)(bw.BaseStream.Position - dataPosition),
                    unk1 = 1
                });

                fileOffset = (int)(bw.BaseStream.Position - dataPosition);
            }

            var fimbHeader = new GarcFimbHeader
            {
                magic = "BMIF",
                headerSize = 0xC,
                dataSize = (uint)(bw.BaseStream.Length - dataPosition)
            };

            bw.BaseStream.Position = fimbPosition;
            WriteFimbHeader(fimbHeader, bw);

            // Write file entries
            bw.BaseStream.Position = fatbPosition + FatbHeaderSize_;

            var fatOffsets = new List<int>();
            var fatbOffset = 0;
            foreach (var entry in fileEntries)
            {
                WriteFatbEntry(entry, bw);
                fatOffsets.Add(fatbOffset);

                fatbOffset += FatbEntrySize_;
            }

            var fatbHeader = new GarcFatbHeader
            {
                magic = "BTAF",
                sectionSize = FatbHeaderSize_ + fileEntries.Count * FatbEntrySize_,
                entryCount = fileEntries.Count
            };

            bw.BaseStream.Position = fatbPosition;
            WriteFatbHeader(fatbHeader, bw);

            // Write FAT Offsets
            var fatoHeader = new GarcFatoHeader
            {
                magic = "OTAF",
                sectionSize = FatoHeaderSize_ + fatOffsets.Count * 4,
                entryCount = (short)fatOffsets.Count,
                unk1 = 0xFFFF
            };

            bw.BaseStream.Position = fatOffsetPosition;
            WriteFatoHeader(fatoHeader, bw);
            WriteIntegers(fatOffsets, bw);

            // Write GARC Header
            var header = new Garc2Header
            {
                magic = "CRAG",
                byteOrder = (ushort)_byteOrder,
                dataOffset = (uint)dataPosition,
                fileSize = (uint)bw.BaseStream.Length,
                headerSize = HeaderSize_,
                major = 2,
                secCount = 4
            };

            bw.BaseStream.Position = 0;
            WriteHeader(header, bw);
        }

        private Garc2Header ReadHeader(BinaryReaderX reader)
        {
            return new Garc2Header
            {
                magic = reader.ReadString(4),
                headerSize = reader.ReadUInt32(),
                byteOrder = reader.ReadUInt16(),
                minor = reader.ReadByte(),
                major = reader.ReadByte(),
                secCount = reader.ReadUInt32(),
                dataOffset = reader.ReadUInt32(),
                fileSize = reader.ReadUInt32()
            };
        }

        private GarcFatoHeader ReadFatoHeader(BinaryReaderX reader)
        {
            return new GarcFatoHeader
            {
                magic = reader.ReadString(4),
                sectionSize = reader.ReadInt32(),
                entryCount = reader.ReadInt16(),
                unk1 = reader.ReadUInt16()
            };
        }

        private GarcFatbHeader ReadFatbHeader(BinaryReaderX reader)
        {
            return new GarcFatbHeader
            {
                magic = reader.ReadString(4),
                sectionSize = reader.ReadInt32(),
                entryCount = reader.ReadInt32()
            };
        }

        private Garc2FatbEntry ReadFatbEntry(BinaryReaderX reader)
        {
            return new Garc2FatbEntry
            {
                unk1 = reader.ReadInt32(),
                offset = reader.ReadUInt32(),
                nextFileOffset = reader.ReadUInt32()
            };
        }

        private GarcFimbHeader ReadFimbHeader(BinaryReaderX reader)
        {
            return new GarcFimbHeader
            {
                magic = reader.ReadString(4),
                headerSize = reader.ReadUInt32(),
                dataSize = reader.ReadUInt32()
            };
        }

        private int[] ReadIntegers(BinaryReaderX reader, int count)
        {
            var result = new int[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadInt32();

            return result;
        }

        private void WriteHeader(Garc2Header header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.headerSize);
            writer.Write(header.byteOrder);
            writer.Write(header.minor);
            writer.Write(header.major);
            writer.Write(header.secCount);
            writer.Write(header.dataOffset);
            writer.Write(header.fileSize);
        }

        private void WriteFatoHeader(GarcFatoHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.sectionSize);
            writer.Write(header.entryCount);
            writer.Write(header.unk1);
        }

        private void WriteFatbHeader(GarcFatbHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.sectionSize);
            writer.Write(header.entryCount);
        }

        private void WriteFatbEntry(Garc2FatbEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.unk1);
            writer.Write(entry.offset);
            writer.Write(entry.nextFileOffset);
        }

        private void WriteFimbHeader(GarcFimbHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.headerSize);
            writer.Write(header.dataSize);
        }

        private void WriteIntegers(IList<int> entries, BinaryWriterX writer)
        {
            foreach (int entry in entries)
                writer.Write(entry);
        }
    }
}
