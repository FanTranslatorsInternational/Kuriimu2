using System.Collections.Generic;
using Kanvas.Encoding;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.IO;

namespace plugin_yuusha_shisu.BTX
{
    public static class BtxSupport
    {
        public static IDictionary<int, IColorEncoding> Encodings = new Dictionary<int, IColorEncoding>
        {
            [0] = new Rgba(8, 8, 8, 8, ByteOrder.BigEndian)
        };

        public static IDictionary<int, IColorIndexEncoding> IndexEncodings = new Dictionary<int, IColorIndexEncoding>
        {
            [5] = new Index(8)
        };

        /// <summary>
        /// 
        /// </summary>
        public static Dictionary<int, IColorEncoding> PaletteEncodings = new Dictionary<int, IColorEncoding>
        {
            [5] = new Rgba(8, 8, 8, 8, ByteOrder.BigEndian)
        };
    }

    /// <summary>
    /// 
    /// </summary>
    [Alignment(0x10)]
    public class FileHeader
    {
        [FixedLength(0x4)]
        public string Magic;
        public int ColorCount; // Palette size?
        public short Width;
        public short Height;
        public int Unk1; // Width/Height again?
        public ImageFormat Format;
        public short Unk3; // Palette size?
        public int Unk4;
        public int ImageOffset;
        public int Unk5;
        public int PaletteOffset;
        public int Unk6; // 0x30?
    }

    /// <summary>
    /// 
    /// </summary>
    public enum ImageFormat : short
    {
        RGBA8888 = 0x00,
        Palette_8 = 0x05
    }
}
