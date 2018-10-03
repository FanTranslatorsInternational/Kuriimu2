using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using Kanvas;
using Kanvas.Format;
using Kanvas.Interface;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Interfaces;

namespace Kore.SamplePlugins
{
    /// <summary>
    /// Marker interface.
    /// </summary>
    public interface IMtFrameworkTextureAdapter { }

    public partial class MTTEX
    {
        // Format
        public enum Format : byte
        {
            // PS3
            DXT1_Remap = 0x19,
            DXT5_B = 0x21,
            DXT5_C = 0x27,
            DXT5_YCbCr = 0x2A
        }

        public enum Version : short
        {
            _PS3v1 = 0x9a,

            _3DSv1 = 0xa4,
            _3DSv2 = 0xa5,
            _3DSv3 = 0xa6,

            _Switchv1 = 0xa0
        }

        // This particular enum is questionable as the data space for it is only 4-bits (maybe)
        public enum AlphaChannelFlags : byte
        {
            Normal = 0x0,
            YCbCrTransform = 0x02,
            Unknown1 = 0x03,
            Unknown2 = 0x04,
            Mixed = 0x08,
            NormalMaps = 0x0B, // ?
            MirroredNormalMaps1 = 0x13, // ?
            MirroredNormalMaps2 = 0x1B, // ?
            CTMipTexture = 0x20 // ?
        }

        public class FileHeader
        {
            [FixedLength(4)]
            public string Magic;
            public uint Block1;
            // Version 12-bit
            // Unknown1 12-bit
            // Unused1 4-bit
            // AlphaChannelFlags 4-bit
            public uint Block2;
            // MipMapCount 6-bit
            // Width 13-bit
            // Height 13-bit
            public uint Block3;
            // Unknown2 8-bit
            // Format 8-bit
            // Unknown3 16-bit
        }

        public class FileHeaderInfo
        {
            // Block 1
            public Version Version;
            public int Unknown1;
            public int Unused1;
            public AlphaChannelFlags AlphaChannelFlags;

            // Block 2
            public int MipMapCount;
            public int Width;
            public int Height;

            // Block 3
            public int Unknown2;
            public byte Format;
            public int Unknown3;
        }

        public sealed class MTTexBitmapInfo : BitmapInfo
        {
            [Category("Properties")]
            [ReadOnly(true)]
            public string Format { get; set; }
        }

        public static Dictionary<byte, IImageFormat> Formats = new Dictionary<byte, IImageFormat>
        {
            [1] = new RGBA(4, 4, 4, 4),
            [2] = new RGBA(5, 5, 5, 1),
            [3] = new RGBA(8, 8, 8, 8),
            [4] = new RGBA(5, 6, 5),
            [7] = new LA(8, 8),
            [11] = new ETC1(false, true),
            [12] = new ETC1(true, true),
            [14] = new LA(0, 4),
            [15] = new LA(4, 0),
            [16] = new LA(4, 4),
            [17] = new RGBA(8, 8, 8),

            [19] = new DXT(DXT.Format.DXT1),
            [20] = new DXT(DXT.Format.DXT3),
            [23] = new DXT(DXT.Format.DXT5),
            [25] = new DXT(DXT.Format.DXT1),
            [33] = new DXT(DXT.Format.DXT5),
            [39] = new DXT(DXT.Format.DXT5),
            [42] = new DXT(DXT.Format.DXT5)
        };

        public static Dictionary<byte, IImageFormat> SwitchFormats = new Dictionary<byte, IImageFormat>
        {
            [0x07] = new RGBA(8, 8, 8, 8, false, false, ByteOrder.BigEndian),
            [0x13] = new DXT(DXT.Format.DXT1),
            [0x17] = new DXT(DXT.Format.DXT5),
            [0x19] = new ATI(ATI.Format.ATI1A),
            [0x1F] = new ATI(ATI.Format.ATI2)
        };

        public class BlockSwizzle : IImageSwizzle
        {
            private MasterSwizzle _swizzle;

            public int Width { get; }
            public int Height { get; }

            public BlockSwizzle(int width, int height)
            {
                Width = (width + 3) & ~3;
                Height = (height + 3) & ~3;

                _swizzle = new MasterSwizzle(Width, new Point(0, 0), new[] { (1, 0), (2, 0), (0, 1), (0, 2) });
            }

            public Point Get(Point point) => _swizzle.Get(point.Y * Width + point.X);
        }

        // Currently trying out YCbCr:
        // https://en.wikipedia.org/wiki/YCbCr#JPEG_conversion
        private const int CbCrThreshold = 123; // usually 128, but 123 seems to work better here

        public static Color ToNoAlpha(Color c)
        {
            return Color.FromArgb(255, c.R, c.G, c.B);
        }

        public static Color ToProperColors(Color c)
        {
            var (A, Y, Cb, Cr) = (c.G, c.A, c.B - CbCrThreshold, c.R - CbCrThreshold);
            return Color.FromArgb(A,
                Common.Clamp(Y + 1.402 * Cr),
                Common.Clamp(Y - 0.344136 * Cb - 0.714136 * Cr),
                Common.Clamp(Y + 1.772 * Cb));
        }

        public static Color ToOptimisedColors(Color c)
        {
            var (A, Y, Cb, Cr) = (c.A,
                0.299 * c.R + 0.587 * c.G + 0.114 * c.B,
                CbCrThreshold - 0.168736 * c.R - 0.331264 * c.G + 0.5 * c.B,
                CbCrThreshold + 0.5 * c.R - 0.418688 * c.G - 0.081312 * c.B);
            return Color.FromArgb(Common.Clamp(Y), Common.Clamp(Cr), A, Common.Clamp(Cb));
        }
    }
}
