using System.Collections.Generic;
using System.Drawing;
using Kanvas;
using Kanvas.Encoding;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;
using Kontract.Models.IO;

#pragma warning disable 649

namespace plugin_nintendo.Images
{
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

    public class CtpkSupport
    {
        public static Dictionary<int, IColorEncoding> CtrFormat = new Dictionary<int, IColorEncoding>
        {
            [0] = ImageFormats.Rgba8888(),
            [1] = ImageFormats.Rgb888(),
            [2] = ImageFormats.Rgba5551(),
            [3] = ImageFormats.Rgb565(),
            [4] = ImageFormats.Rgba4444(),
            [5] = ImageFormats.La44(),
            [6] = ImageFormats.Rgb888(),
            [7] = ImageFormats.L8(),
            [8] = ImageFormats.A8(),
            [9] = ImageFormats.La44(),
            [10] = ImageFormats.L4(BitOrder.LeastSignificantBitFirst),
            [11] = ImageFormats.A4(BitOrder.LeastSignificantBitFirst),
            [12] = ImageFormats.Etc1(true),
            [13] = ImageFormats.Etc1A4(true)
        };
    }

    class CtpkImageInfo : ImageInfo
    {
        public TexEntry Entry { get; }

        public MipmapEntry MipEntry { get; }

        public CtpkImageInfo(byte[] imageData, int imageFormat, Size imageSize, TexEntry entry, MipmapEntry mipEntry) : base(imageData, imageFormat, imageSize)
        {
            Entry = entry;
            MipEntry = mipEntry;
        }

        public CtpkImageInfo(byte[] imageData, IList<byte[]> mipMaps, int imageFormat, Size imageSize, TexEntry entry, MipmapEntry mipEntry) : base(imageData, mipMaps, imageFormat, imageSize)
        {
            Entry = entry;
            MipEntry = mipEntry;
        }
    }
}
