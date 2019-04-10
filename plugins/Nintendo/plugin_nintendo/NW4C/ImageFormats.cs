using System.Collections.Generic;
using Kanvas.Format;
using Kanvas.Interface;
using Komponent.IO;

namespace plugin_nintendo.NW4C
{
    public static class ImageFormats
    {
        /// <summary>
        /// Image format IDs and declarations for the 3DS.
        /// </summary>
        public static Dictionary<byte, IImageFormat> CTRFormats = new Dictionary<byte, IImageFormat>
        {
            [0] = new LA(8, 0),
            [1] = new LA(0, 8),
            [2] = new LA(4, 4),
            [3] = new LA(8, 8),
            [4] = new HL(8, 8),
            [5] = new RGBA(5, 6, 5),
            [6] = new RGBA(8, 8, 8),
            [7] = new RGBA(5, 5, 5, 1),
            [8] = new RGBA(4, 4, 4, 4),
            [9] = new RGBA(8, 8, 8, 8),
            [10] = new ETC1(),
            [11] = new ETC1(true),
            [18] = new LA(4, 0),
            [19] = new LA(0, 4),
        };

        /// <summary>
        /// Image format IDs and declarations for the WiiU.
        /// </summary>
        public static Dictionary<byte, IImageFormat> CafeFormats = new Dictionary<byte, IImageFormat>
        {
            [0] = new LA(8, 0, ByteOrder.BigEndian),
            [1] = new LA(0, 8, ByteOrder.BigEndian),
            [2] = new LA(4, 4, ByteOrder.BigEndian),
            [3] = new LA(8, 8, ByteOrder.BigEndian),
            [4] = new HL(8, 8, ByteOrder.BigEndian),
            [5] = new RGBA(5, 6, 5, 0, false, false, ByteOrder.BigEndian),
            [6] = new RGBA(8, 8, 8, 0, false, false, ByteOrder.BigEndian),
            [7] = new RGBA(5, 5, 5, 1, false, false, ByteOrder.BigEndian),
            [8] = new RGBA(4, 4, 4, 4, false, false, ByteOrder.BigEndian),
            [9] = new RGBA(8, 8, 8, 8, false, false, ByteOrder.BigEndian),
            [10] = new ETC1(false, false, ByteOrder.BigEndian),
            [11] = new ETC1(true, false, ByteOrder.BigEndian),
            [12] = new DXT(DXT.Format.DXT1),
            [13] = new DXT(DXT.Format.DXT3),
            [14] = new DXT(DXT.Format.DXT5),
            [15] = new ATI(ATI.Format.ATI1L),
            [16] = new ATI(ATI.Format.ATI1A),
            [17] = new ATI(ATI.Format.ATI2),
            [18] = new LA(4, 0, ByteOrder.BigEndian),
            [19] = new LA(0, 4, ByteOrder.BigEndian),
            [20] = new RGBA(8, 8, 8, 8, false, false, ByteOrder.BigEndian),
            [21] = new DXT(DXT.Format.DXT1),
            [22] = new DXT(DXT.Format.DXT3),
            [23] = new DXT(DXT.Format.DXT5),
            [24] = new RGBA(10, 10, 10, 2, false, false, ByteOrder.BigEndian)
        };
    }
}
