using System.Buffers.Binary;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Plugin.File.Archive;
using plugin_nintendo.Common.Compression;

namespace plugin_nintendo.Archives
{
    struct Garc2Header
    {
        public string magic; // CRAG
        public uint headerSize;
        public ushort byteOrder;
        public byte minor;
        public byte major; // 2
        public uint secCount; // 4
        public uint dataOffset;
        public uint fileSize;
        // misses largest file size from GARC4
    }

    struct Garc4Header
    {
        public string magic;// CRAG
        public uint headerSize;
        public ushort byteOrder;
        public byte minor;
        public byte major; // 4
        public uint secCount; // 4
        public uint dataOffset;
        public uint fileSize;
        public uint largestFileSize;
    }

    struct GarcFatoHeader
    {
        public string magic; // OTAF
        public int sectionSize;
        public short entryCount;
        public ushort unk1; // 0xFFFF
    }

    struct GarcFatbHeader
    {
        public string magic; // BTAF
        public int sectionSize;
        public int entryCount;
    }

    struct Garc2FatbEntry
    {
        public int unk1; // 1
        public uint offset;
        public uint nextFileOffset;
        // misses size from GARC4
    }

    struct Garc4FatbEntry
    {
        public int unk1; // 1
        public uint offset;
        public uint nextFileOffset;
        public uint size;
    }

    struct GarcFimbHeader
    {
        public string magic; // BMIF
        public uint headerSize; // 0xC
        public uint dataSize;
    }

    static class GarcSupport
    {
        public static IArchiveFile CreateAfi(Stream file, string fileName)
        {
            var compressionIdent = file.ReadByte();
            var isCompressed = compressionIdent == 0x10 ||
                               compressionIdent == 0x11 ||
                               compressionIdent == 0x24 ||
                               compressionIdent == 0x28 ||
                               compressionIdent == 0x30;

            file.Position--;
            if (!isCompressed)
                return new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = file
                });

            var sizeBuffer = new byte[4];
            file.Read(sizeBuffer, 0, 4);
            file.Position = 0;

            var method = (NintendoCompressionMethod)(BinaryPrimitives.ReadUInt32LittleEndian(sizeBuffer) & 0xFF);
            var decompressedSize = BinaryPrimitives.ReadUInt32LittleEndian(sizeBuffer) >> 8;

            if (decompressedSize <= file.Length)
                return new ArchiveFile(new ArchiveFileInfo
                {
                    FilePath = fileName,
                    FileData = file
                });


            return new ArchiveFile(new CompressedArchiveFileInfo
            {
                FilePath = fileName,
                FileData = file,
                Compression = NintendoCompressor.GetCompression(method),
                DecompressedSize = (int)decompressedSize
            });
        }
    }
}
