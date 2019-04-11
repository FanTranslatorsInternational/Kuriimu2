using System.Collections.Generic;
using System.Drawing;
using Kanvas.Format;
using Kanvas.Interface;
using Komponent.IO;
using Komponent.IO.Attributes;
using Kontract.Interfaces.Image;

namespace plugin_blue_reflection.KSLT
{
    /// <summary>
    /// 
    /// </summary>
    public class FileHeader
    {
        [FixedLength(8)]
        public string Magic;
        public int FileCount;
        public int FileSize;
        public int OffsetTable;
        public int FNameTableSize;
        public int FileCount2;
    }

    /// <summary>
    /// 
    /// </summary>
    public class UnkPadding
    {
        [FixedLength(0x38)]
        public byte[] Padding = {0x0B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80,
                                 0x3F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00,
                                 0x80, 0x3F, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
    }

    /// <summary>
    /// 
    /// </summary>
    public class OffsetEntry
    {
        public int Offset;
        [FixedLength(0x10)]
        public byte[] Padding;
    }

    /// <summary>
    /// 
    /// </summary>
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

    /// <summary>
    /// 
    /// </summary>
    public static class ImageFormats
    {
        /// <summary>
        /// 
        /// </summary>
        public static Dictionary<int, IImageFormat> Formats = new Dictionary<int, IImageFormat>
        {
            [0x0] = new RGBA(8, 8, 8, 8) { IsAlphaFirst = true }
        };
    }

    public class KsltBitmapInfo : BitmapInfo
    {
        public ImageHeader Header { get; set; }

        public KsltBitmapInfo(Bitmap image, FormatInfo formatInfo) : base(image, formatInfo) { }
    }
}