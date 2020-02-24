//using System.Collections.Generic;
//using Kanvas.Encoding;
//using Kanvas.Encoding.BlockCompressions.BCn.Models;
//using Kontract.Kanvas;
//using Kontract.Models.IO;

//namespace plugin_nintendo.NW4C
//{
//    public static class ImageFormats
//    {
//        /// <summary>
//        /// Image format IDs and declarations for the 3DS.
//        /// </summary>
//        public static readonly Dictionary<byte, IColorEncoding> CtrFormats = new Dictionary<byte, IColorEncoding>
//        {
//            [0] = new LA(8, 0),
//            [1] = new LA(0, 8),
//            [2] = new LA(4, 4),
//            [3] = new LA(8, 8),
//            [4] = new RGBA(8, 8, 0),
//            [5] = new RGBA(5, 6, 5),
//            [6] = new RGBA(8, 8, 8),
//            [7] = new RGBA(5, 5, 5, 1),
//            [8] = new RGBA(4, 4, 4, 4),
//            [9] = new RGBA(8, 8, 8, 8),
//            [10] = new ETC1(false, true, ByteOrder.LittleEndian, 8),
//            [11] = new ETC1(true, true, ByteOrder.LittleEndian, 8),
//            [18] = new LA(4, 0),
//            [19] = new LA(0, 4),
//        };

//        /// <summary>
//        /// Image format IDs and declarations for the WiiU.
//        /// </summary>
//        // TODO: Rethink color formats, since they wouldn't work on BE systems like that
//        public static readonly Dictionary<byte, IColorEncoding> CafeFormats = new Dictionary<byte, IColorEncoding>
//        {
//            [0] = new LA(8, 0, "AL"),
//            [1] = new LA(0, 8, "AL"),
//            [2] = new LA(4, 4, "AL"),
//            [3] = new LA(8, 8, "AL"),
//            [4] = new RGBA(8, 8, 0, 0, "ABGR"),
//            [5] = new RGBA(5, 6, 5, 0, "ABGR"),
//            [6] = new RGBA(8, 8, 8, 0, "ABGR"),
//            [7] = new RGBA(5, 5, 5, 1, "ABGR"),
//            [8] = new RGBA(4, 4, 4, 4, "ABGR"),
//            [9] = new RGBA(8, 8, 8, 8, "ABGR"),
//            [10] = new ETC1(false, false, ByteOrder.BigEndian, 8),
//            [11] = new ETC1(true, false, ByteOrder.BigEndian, 8),
//            [12] = new BC(BcFormat.DXT1, ByteOrder.BigEndian, 8),
//            [13] = new BC(BcFormat.DXT3, ByteOrder.BigEndian, 8),
//            [14] = new BC(BcFormat.DXT5, ByteOrder.BigEndian, 8),
//            [15] = new BC(BcFormat.ATI1L_WiiU, ByteOrder.BigEndian, 8),
//            [16] = new BC(BcFormat.ATI1A_WiiU, ByteOrder.BigEndian, 8),
//            [17] = new BC(BcFormat.ATI2, ByteOrder.BigEndian, 8),
//            [18] = new LA(4, 0, "AL"),
//            [19] = new LA(0, 4, "AL"),
//            [20] = new RGBA(8, 8, 8, 8, "ABGR"),
//            [21] = new BC(BcFormat.DXT1, ByteOrder.BigEndian, 8),
//            [22] = new BC(BcFormat.DXT3, ByteOrder.BigEndian, 8),
//            [23] = new BC(BcFormat.DXT5, ByteOrder.BigEndian, 8),
//            [24] = new RGBA(10, 10, 10, 2, "ABGR"),
//        };
//    }
//}
