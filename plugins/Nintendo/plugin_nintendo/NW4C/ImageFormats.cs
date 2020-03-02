using System.Collections.Generic;
using Kanvas.Encoding;
using Kanvas.Encoding.BlockCompressions.BCn.Models;
using Kontract.Kanvas;
using Kontract.Models.IO;

namespace plugin_nintendo.NW4C
{
    public static class ImageFormats
    {
        /// <summary>
        /// Image format IDs and declarations for the 3DS.
        /// </summary>
        public static readonly Dictionary<int, IColorEncoding> CtrFormats = new Dictionary<int, IColorEncoding>
        {
            [0] = new La(8, 0),
            [1] = new La(0, 8),
            [2] = new La(4, 4),
            [3] = new La(8, 8),
            [4] = new Rgba(8, 8, 0),
            [5] = new Rgba(5, 6, 5),
            [6] = new Rgba(8, 8, 8),
            [7] = new Rgba(5, 5, 5, 1),
            [8] = new Rgba(4, 4, 4, 4),
            [9] = new Rgba(8, 8, 8, 8),
            [10] = new Etc1(false, true),
            [11] = new Etc1(true, true),
            [18] = new La(4, 0),
            [19] = new La(0, 4),
        };

        /// <summary>
        /// Image format IDs and declarations for the WiiU.
        /// </summary>
        public static readonly Dictionary<int, IColorEncoding> CafeFormats = new Dictionary<int, IColorEncoding>
        {
            [0] = new La(8, 0, ByteOrder.BigEndian),
            [1] = new La(0, 8, ByteOrder.BigEndian),
            [2] = new La(4, 4, ByteOrder.BigEndian),
            [3] = new La(8, 8, ByteOrder.BigEndian),
            [4] = new Rgba(8, 8, 0, ByteOrder.BigEndian),
            [5] = new Rgba(5, 6, 5, ByteOrder.BigEndian),
            [6] = new Rgba(8, 8, 8, ByteOrder.BigEndian),
            [7] = new Rgba(5, 5, 5, 1, ByteOrder.BigEndian),
            [8] = new Rgba(4, 4, 4, 4, ByteOrder.BigEndian),
            [9] = new Rgba(8, 8, 8, 8, ByteOrder.BigEndian),
            [10] = new Etc1(false, false, ByteOrder.BigEndian),
            [11] = new Etc1(true, false, ByteOrder.BigEndian),
            [12] = new Bc(BcFormat.DXT1, ByteOrder.BigEndian),
            [13] = new Bc(BcFormat.DXT3, ByteOrder.BigEndian),
            [14] = new Bc(BcFormat.DXT5, ByteOrder.BigEndian),
            [15] = new Bc(BcFormat.ATI1L_WiiU, ByteOrder.BigEndian),
            [16] = new Bc(BcFormat.ATI1A_WiiU, ByteOrder.BigEndian),
            [17] = new Bc(BcFormat.ATI2, ByteOrder.BigEndian),
            [18] = new La(4, 0),
            [19] = new La(0, 4),

            // sRGB formats
            [20] = new Rgba(8, 8, 8, 8),
            [21] = new Bc(BcFormat.DXT1, ByteOrder.BigEndian),
            [22] = new Bc(BcFormat.DXT3, ByteOrder.BigEndian),
            [23] = new Bc(BcFormat.DXT5, ByteOrder.BigEndian),

            [24] = new Rgba(10, 10, 10, 2)
        };
    }
}
