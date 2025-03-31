using Kanvas;
using Kanvas.Contract.Encoding;
using Komponent.Contract.Enums;

namespace plugin_nintendo.NW4C
{
    public static class Nw4cImageFormats
    {
        /// <summary>
        /// Image format IDs and declarations for the 3DS.
        /// </summary>
        public static readonly Dictionary<int, IColorEncoding> CtrFormats = new Dictionary<int, IColorEncoding>
        {
            [0] = ImageFormats.L8(),
            [1] = ImageFormats.A8(),
            [2] = ImageFormats.La44(),
            [3] = ImageFormats.La88(),
            [4] = ImageFormats.Rg88(),
            [5] = ImageFormats.Rgb565(),
            [6] = ImageFormats.Rgb888(),
            [7] = ImageFormats.Rgba5551(),
            [8] = ImageFormats.Rgba4444(),
            [9] = ImageFormats.Rgba8888(),
            [10] = ImageFormats.Etc1(true),
            [11] = ImageFormats.Etc1A4(true),

            [12] = ImageFormats.L4(BitOrder.LeastSignificantBitFirst),
            [13] = ImageFormats.L4(BitOrder.LeastSignificantBitFirst),

            [18] = ImageFormats.L4(BitOrder.LeastSignificantBitFirst),
            [19] = ImageFormats.A4(BitOrder.LeastSignificantBitFirst)
        };

        /// <summary>
        /// Image format IDs and declarations for the WiiU.
        /// </summary>
        public static readonly Dictionary<int, IColorEncoding> CafeFormats = new Dictionary<int, IColorEncoding>
        {
            [0] = ImageFormats.L8(),
            [1] = ImageFormats.A8(),
            [2] = ImageFormats.La44(),
            [3] = ImageFormats.La88(ByteOrder.BigEndian),
            [4] = ImageFormats.Rg88(ByteOrder.BigEndian),
            [5] = ImageFormats.Rgb565(ByteOrder.BigEndian),
            [6] = ImageFormats.Rgb888(),
            [7] = ImageFormats.Rgba5551(ByteOrder.BigEndian),
            [8] = ImageFormats.Rgba4444(ByteOrder.BigEndian),
            [9] = ImageFormats.Rgba8888(ByteOrder.BigEndian),
            [10] = ImageFormats.Etc1(false),
            [11] = ImageFormats.Etc1A4(false),
            [12] = ImageFormats.Dxt1(),
            [13] = ImageFormats.Dxt3(),
            [14] = ImageFormats.Dxt5(),
            [15] = ImageFormats.Ati1L(),
            [16] = ImageFormats.Ati1A(),
            [17] = ImageFormats.Ati2AL(),
            [18] = ImageFormats.L4(),
            [19] = ImageFormats.A4(),

            // sRGB formats
            [20] = ImageFormats.Rgba8888(ByteOrder.BigEndian),
            [21] = ImageFormats.Dxt1(),
            [22] = ImageFormats.Dxt3(),
            [23] = ImageFormats.Dxt5(),

            [24] = ImageFormats.Rgba1010102(ByteOrder.BigEndian)
        };
    }
}
