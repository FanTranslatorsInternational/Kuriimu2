using System.Buffers.Binary;
using System.IO;
using Komponent.IO.Attributes;
using Kontract.Models.Archive;
using Kontract.Models.IO;
using plugin_level5.Compression;
using plugin_nintendo.Compression;

namespace plugin_nintendo.Archives
{
    class Garc2Header
    {
        [FixedLength(4)]
        public string magic = "CRAG";
        public uint headerSize;
        [Endianness(ByteOrder = ByteOrder.BigEndian)]
        public ushort byteOrder;
        public byte minor;
        public byte major = 2;
        public uint secCount = 4;
        public uint dataOffset;
        public uint fileSize;
        // misses largest file size from GARC4
    }

    class Garc4Header
    {
        [FixedLength(4)]
        public string magic = "CRAG";
        public uint headerSize;
        [Endianness(ByteOrder = ByteOrder.BigEndian)]
        public ushort byteOrder;
        public byte minor;
        public byte major = 4;
        public uint secCount = 4;
        public uint dataOffset;
        public uint fileSize;
        public uint largestFileSize;
    }

    class GarcFatoHeader
    {
        [FixedLength(4)]
        public string magic = "OTAF";
        public int sectionSize;
        public short entryCount;
        public ushort unk1 = 0xFFFF;
    }

    class GarcFatbHeader
    {
        [FixedLength(4)]
        public string magic = "BTAF";
        public int sectionSize;
        public int entryCount;
    }

    class Garc2FatbEntry
    {
        public int unk1 = 1;
        public uint offset;
        public uint nextFileOffset;
        // misses size from GARC4
    }

    class Garc4FatbEntry
    {
        public int unk1 = 1;
        public uint offset;
        public uint nextFileOffset;
        public uint size;
    }

    class GarcFimbHeader
    {
        [FixedLength(4)]
        public string magic = "BMIF";
        public uint headerSize = 0xC;
        public uint dataSize;
    }

    static class GarcSupport
    {
        public static ArchiveFileInfo CreateAfi(Stream file, string fileName)
        {
            var compressionIdent = file.ReadByte();
            var isCompressed = compressionIdent == 0x10 ||
                               compressionIdent == 0x11 ||
                               compressionIdent == 0x24 ||
                               compressionIdent == 0x28 ||
                               compressionIdent == 0x30;

            file.Position--;
            if (!isCompressed)
                return new ArchiveFileInfo(file, fileName);

            var sizeBuffer = new byte[4];
            file.Read(sizeBuffer, 0, 4);
            file.Position = 0;

            var method = (NintendoCompressionMethod)(BinaryPrimitives.ReadUInt32LittleEndian(sizeBuffer) & 0xFF);
            var decompressedSize = BinaryPrimitives.ReadUInt32LittleEndian(sizeBuffer) >> 8;

            if (decompressedSize <= file.Length)
                return new ArchiveFileInfo(file, fileName);

            return new ArchiveFileInfo(file, fileName, NintendoCompressor.GetConfiguration(method), decompressedSize);
        }
    }
}
