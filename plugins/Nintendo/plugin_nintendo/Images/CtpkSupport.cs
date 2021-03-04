using System.Collections.Generic;
using System.Drawing;
using Kanvas.Encoding;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;

#pragma warning disable 649

namespace plugin_nintendo.Images
{
    public class CtpkSupport
    {
        public static Dictionary<int, IColorEncoding> CtrFormat = new Dictionary<int, IColorEncoding>
        {
            [0] = new Rgba(8, 8, 8, 8),
            [1] = new Rgba(8, 8, 8),
            [2] = new Rgba(5, 5, 5, 1),
            [3] = new Rgba(5, 6, 5),
            [4] = new Rgba(4, 4, 4, 4),
            [5] = new La(8, 8),
            [6] = new Rgba(8, 8, 0),
            [7] = new La(8, 0),
            [8] = new La(0, 8),
            [9] = new La(4, 4),
            [10] = new La(4, 0),
            [11] = new La(0, 4),
            [12] = new Etc1(false, true),
            [13] = new Etc1(true, true)
        };
    }

    public class CtpkHeader
    {
        [FixedLength(4)] 
        public string magic = "CTPK";
        public short version;
        public short texCount;
        public int texSecOffset;
        public int texSecSize;
        public int crc32SecOffset;
        public int texInfoOffset;
    }

    public class TexEntry
    {
        public int nameOffset;
        public int texDataSize;
        public int texOffset;
        public int imageFormat;
        public short width;
        public short height;
        public byte mipLvl;
        public byte type;
        public short zero0;
        public int sizeOffset;
        public uint timeStamp;
    }

    public class HashEntry
    {
        public uint crc32;
        public int id;
    }

    public class MipmapEntry
    {
        public byte mipmapFormat;
        public byte mipLvl;
        //never used compression specifications?
        public byte compression;
        public byte compMethod;
    }

    class CtpkImageInfo : ImageInfo
    {
        public TexEntry Entry { get; }

        public MipmapEntry MipEntry { get; }

        public CtpkImageInfo(byte[] imageData, int imageFormat, Size imageSize,TexEntry entry,MipmapEntry mipEntry) : base(imageData, imageFormat, imageSize)
        {
            Entry = entry;
            MipEntry = mipEntry;
        }

        public CtpkImageInfo(byte[] imageData, IList<byte[]> mipMaps, int imageFormat, Size imageSize,TexEntry entry, MipmapEntry mipEntry) : base(imageData, mipMaps, imageFormat, imageSize)
        {
            Entry = entry;
            MipEntry = mipEntry;
        }
    }
}
