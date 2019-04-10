using Kanvas.Format;
using Kanvas.Interface;
using Komponent.IO.Attributes;
using System.Collections.Generic;

namespace plugin_blue_reflection.KSLT
{
    public class FileHeader
    {
        [FixedLength(8)]
        public string Magic;
        public int FileCount;
        public int FileSize;
        public int OffsetTable;
        public int FNameTableSize;
        public int FileSize2;
    }
    public class unkPadding
    {
        [FixedLength(0x38)]
        public byte[] Padding = {0x0B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80,
                                 0x3F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00,
                                 0x80, 0x3F, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
    }
    public class OffsetEntry
    {
        public int Offset;
        [FixedLength(0x10)]
        public byte[] Padding;
    }
    public class ImageHeader
    {
        public int unk0;
        public short Width;
        public short Height;
        public int unk3;
        public int unk4;
        public int unk5;
        public int unk6;
        public int unk7;
        public int DataSize;
        public int unk8;
        [FixedLength(0x24)]
        public byte[] Padding;
    }
    public static class ImageFormats
    {
        public static Dictionary<int, IImageFormat> Formats = new Dictionary<int, IImageFormat>
        {
            [0x0] = new RGBA(8, 8, 8, 8, false, true),
        };
    }
}