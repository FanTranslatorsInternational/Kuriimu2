using Komponent.Contract.Enums;
using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.Plugin.File.Archive;

namespace plugin_nintendo.Archives
{
    public class Garc4
    {
        private const int HeaderSize_ = 0x1C;
        private const int FatoHeaderSize_ = 0xC;
        private const int FatbHeaderSize_ = 0xC;
        private const int FatbEntrySize_ = 0xC;
        private const int FimbHeaderSize_ = 0xC;

        private ByteOrder _byteOrder;

        public List<IArchiveFile> Load(Stream input)
        {
            var typeReader = new BinaryTypeReader();
            using var br = new BinaryReaderX(input, true);

            // Select byte order
            br.ByteOrder = ByteOrder.BigEndian;
            br.BaseStream.Position = 0x8;
            _byteOrder = (ByteOrder)br.ReadUInt16();

            br.ByteOrder = _byteOrder;

            // Read header
            br.BaseStream.Position = 0;
            var header = typeReader.Read<Garc4Header>(br);

            // Read Fat Offsets
            var fatoHeader = typeReader.Read<GarcFatoHeader>(br);
            var offsets = typeReader.ReadMany<int>(br, fatoHeader.entryCount);

            // Read FATB
            var fatbHeader = typeReader.Read<GarcFatbHeader>(br);
            var fatbOffset = br.BaseStream.Position;

            var fatbEntries = new Garc4FatbEntry[fatoHeader.entryCount];
            for (var i = 0; i < fatoHeader.entryCount; i++)
            {
                br.BaseStream.Position = fatbOffset + offsets[i];
                fatbEntries[i] = typeReader.Read<Garc4FatbEntry>(br);
            }

            // Read FIMB
            typeReader.Read<GarcFimbHeader>(br);

            // Add files
            var result = new List<IArchiveFile>();
            for (var i = 0; i < fatbEntries.Length; i++)
            {
                var fileStream = new SubStream(input, header.dataOffset + fatbEntries[i].offset, fatbEntries[i].size);

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

            var typeWriter = new BinaryTypeWriter();
            using var bw = new BinaryWriterX(output, _byteOrder);

            // Write file data
            bw.BaseStream.Position = dataPosition;

            var fileEntries = new List<Garc4FatbEntry>();
            var largestFileSize = 0;
            var fileOffset = 0;
            foreach (var file in files)
            {
                var writtenSize = file.WriteFileData(output);
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
            typeWriter.Write(new GarcFimbHeader
            {
                dataSize = (uint)(bw.BaseStream.Length - dataPosition)
            }, bw);

            // Write file entries
            bw.BaseStream.Position = fatbPosition + FatbHeaderSize_;

            var fatOffsets = new List<uint>();
            var fatbOffset = 0u;
            foreach (var entry in fileEntries)
            {
                typeWriter.Write(entry, bw);
                fatOffsets.Add(fatbOffset);

                fatbOffset += (uint)FatbEntrySize_;
            }

            bw.BaseStream.Position = fatbPosition;
            typeWriter.Write(new GarcFatbHeader
            {
                sectionSize = FatbHeaderSize_ + fileEntries.Count * FatbEntrySize_,
                entryCount = fileEntries.Count
            }, bw);

            // Write FAT Offsets
            bw.BaseStream.Position = fatOffsetPosition;
            typeWriter.Write(new GarcFatoHeader
            {
                sectionSize = FatoHeaderSize_ + fatOffsets.Count * 4,
                entryCount = (short)fatOffsets.Count
            }, bw);
            typeWriter.WriteMany(fatOffsets, bw);

            // Write GARC Header
            bw.BaseStream.Position = 0;
            typeWriter.Write(new Garc4Header
            {
                byteOrder = (ushort)_byteOrder,
                dataOffset = (uint)dataPosition,
                fileSize = (uint)bw.BaseStream.Length,
                headerSize = (uint)HeaderSize_,
                largestFileSize = (uint)largestFileSize
            }, bw);
        }
    }
}
