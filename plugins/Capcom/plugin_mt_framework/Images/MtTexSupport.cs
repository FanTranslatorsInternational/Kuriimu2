using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Kanvas;
using Komponent.IO;
using Komponent.IO.Attributes;
using Kontract.Interfaces.Managers;
using Kontract.Kanvas;
using Kontract.Models.Dialog;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_mt_framework.Images
{
    [BitFieldInfo(BitOrder = BitOrder.LeastSignificantBitFirst, BlockSize = 4)]
    class MtTexHeader
    {
        [FixedLength(4)]
        public string magic;

        [BitField(12)]
        public short version;
        [BitField(12)]
        public short swizzle;   // TODO: Has to be proven, but seems to work for version A3 found on both, PC and Switch. which use different swizzles
        [BitField(4)]
        public byte reserved1;
        [BitField(4)]
        public byte alphaFlags;

        [BitField(6)]
        public byte mipCount;
        [BitField(13)]
        public short width;
        [BitField(13)]
        public short height;

        [BitField(8)]
        public byte imgCount;
        [BitField(8)]
        public byte format;
        [BitField(16)]
        public ushort unk3;
    }

    [BitFieldInfo(BitOrder = BitOrder.LeastSignificantBitFirst, BlockSize = 4)]
    class MobileMtTexHeader
    {
        [FixedLength(4)]
        public string magic;

        [BitField(16)]
        public ushort version;
        [BitField(8)]
        public byte format;
        [BitField(8)]
        public byte unk1;

        [BitField(4)]
        public byte unk2;
        [BitField(28)]
        public int r1;

        [BitField(13)]
        public short width;
        [BitField(13)]
        public short height;
        [BitField(4)]
        public byte mipCount;
        [BitField(2)]
        public byte unk3;
    }

    class MtTexSupport
    {
        public static readonly IDictionary<int, IColorEncoding> CtrFormats = new Dictionary<int, IColorEncoding>
        {
            [0x01] = ImageFormats.Rgba4444(),
            [0x02] = ImageFormats.Rgba5551(),
            [0x03] = ImageFormats.Rgba8888(),
            [0x04] = ImageFormats.Rgb565(),
            [0x05] = ImageFormats.A8(),
            [0x06] = ImageFormats.L8(),
            [0x07] = ImageFormats.La88(),

            [0x0A] = ImageFormats.Rg88(),
            [0x0B] = ImageFormats.Etc1(true),
            [0x0C] = ImageFormats.Etc1A4(true),

            [0x0E] = ImageFormats.A4(BitOrder.LeastSignificantBitFirst),
            [0x0F] = ImageFormats.L4(BitOrder.LeastSignificantBitFirst),
            [0x10] = ImageFormats.La44(),
            [0x11] = ImageFormats.Bgr888()
        };

        public static readonly IDictionary<int, IColorEncoding> Ps3Formats = new Dictionary<int, IColorEncoding>
        {
            [0x01] = ImageFormats.Rgba4444(),
            [0x02] = ImageFormats.Rgba5551(),
            [0x03] = ImageFormats.Rgba8888(),
            [0x04] = ImageFormats.Rgb565(),
            [0x05] = ImageFormats.A8(),
            [0x06] = ImageFormats.L8(),
            [0x07] = ImageFormats.La88(),

            [0x0A] = ImageFormats.Rg88(),
            [0x0B] = ImageFormats.Etc1(true),
            [0x0C] = ImageFormats.Etc1A4(true),

            [0x0E] = ImageFormats.A4(BitOrder.LeastSignificantBitFirst),
            [0x0F] = ImageFormats.L4(BitOrder.LeastSignificantBitFirst),
            [0x10] = ImageFormats.La44(),
            [0x11] = ImageFormats.Rgb888(),

            [0x13] = ImageFormats.Dxt1(),
            [0x14] = ImageFormats.Dxt3(),
            [0x17] = ImageFormats.Dxt5(),

            [0x19] = ImageFormats.Dxt1(),
            [0x1F] = ImageFormats.Dxt5(),
            [0x21] = ImageFormats.Dxt5(),
            [0x27] = ImageFormats.Dxt5(),
            [0x2A] = ImageFormats.Dxt5()
        };

        public static readonly IDictionary<int, IColorEncoding> SwitchFormats = new Dictionary<int, IColorEncoding>
        {
            [0x07] = ImageFormats.Rgba8888(ByteOrder.BigEndian),
            [0x13] = ImageFormats.Dxt1(),
            [0x14] = ImageFormats.Dxt1(),
            [0x17] = ImageFormats.Dxt5(),
            [0x19] = ImageFormats.Ati1A(),
            [0x1F] = ImageFormats.Ati2(),

            [0x2A] = ImageFormats.Bc7()
        };

        public static readonly IDictionary<int, IColorEncoding> PcFormats = new Dictionary<int, IColorEncoding>
        {
            [0x07] = ImageFormats.Rgba8888(ByteOrder.BigEndian),
            [0x13] = ImageFormats.Dxt1(),
            [0x14] = ImageFormats.Dxt1(),
            [0x17] = ImageFormats.Dxt5(),
            [0x19] = ImageFormats.Ati1A(),
            [0x1F] = ImageFormats.Ati2(),

            [0x2A] = ImageFormats.Bc7(),
            [0x36] = ImageFormats.Bc7()
        };

        public static readonly IDictionary<int, IColorEncoding> MobileFormats = new Dictionary<int, IColorEncoding>
        {
            [0x1] = ImageFormats.Rgba8888(ByteOrder.BigEndian),
            [0x7] = ImageFormats.Rgba4444(ByteOrder.BigEndian),
            [0xa] = ImageFormats.Etc1(false, ByteOrder.BigEndian),

            // Used as placeholders for format 0x0C, which defines 3 images of different encodings for different mobile platforms
            [0xFD] = ImageFormats.Dxt5(),
            [0xFE] = ImageFormats.PvrtcA_4bpp(),
            [0xFF] = ImageFormats.AtcInterpolated()
        };

        private static readonly IDictionary<int, IColorShader> ShadersPs3 = new Dictionary<int, IColorShader>
        {
            [0x21] = new MtTex_NoAlphaShader(),
            [0x2A] = new MtTex_YCbCrColorShader()
        };

        private static readonly IDictionary<int, IColorShader> ShadersSwitch = new Dictionary<int, IColorShader>
        {
            [0x2A] = new MtTex_YCbCrColorShader()
        };

        private static readonly IDictionary<int, IColorShader> ShadersPc = new Dictionary<int, IColorShader>
        {
            [0x2A] = new MtTex_YCbCrColorShader()
        };

        public static MtTexPlatform DeterminePlatform(Stream file, IDialogManager dialogManager)
        {
            using var br = new BinaryReaderX(file, true);

            var magic = br.ReadString(4);
            if (magic == "\0XET")
                br.ByteOrder = ByteOrder.BigEndian;

            // Read version
            file.Position = 4;
            var block = br.ReadUInt32();
            file.Position = 0;

            var version = block & 0xFFF;
            var mobileVersion = block & 0xFFFF;

            // Determine platform
            if (magic == "TEX " && mobileVersion == 0x09)
                return MtTexPlatform.Mobile;

            switch (version)
            {
                case 0xa4:
                case 0xa5:
                case 0xa6:
                    return MtTexPlatform.N3DS;

                case 0x87:
                    return MtTexPlatform.Wii;

                case 0x97:
                case 0x9a:
                case 0x9d:
                    return MtTexPlatform.PS3;

                case 0xa0:
                    return MtTexPlatform.Switch;

                case 0xa3:
                    var selection = new DialogField(DialogFieldType.DropDown, "Platform", MtTexPlatform.Pc.ToString(), MtTexPlatform.Pc.ToString(), MtTexPlatform.Switch.ToString());
                    dialogManager.ShowDialog(new[] { selection });

                    return Enum.Parse<MtTexPlatform>(selection.Result);

                default:
                    throw new InvalidOperationException($"MtTex version 0x{version:X2} is not supported.");
            }
        }

        public static EncodingDefinition GetEncodingDefinition(MtTexPlatform platform)
        {
            var definition = new EncodingDefinition();

            switch (platform)
            {
                case MtTexPlatform.N3DS:
                    definition.AddColorEncodings(CtrFormats);
                    break;

                case MtTexPlatform.Switch:
                    definition.AddColorEncodings(SwitchFormats);
                    definition.AddColorShaders(ShadersSwitch);
                    break;

                case MtTexPlatform.PS3:
                    definition.AddColorEncodings(Ps3Formats);
                    definition.AddColorShaders(ShadersPs3);
                    break;

                case MtTexPlatform.Mobile:
                    definition.AddColorEncodings(MobileFormats);
                    break;

                case MtTexPlatform.Pc:
                    definition.AddColorEncodings(PcFormats);
                    definition.AddColorShaders(ShadersPc);
                    break;

                case MtTexPlatform.Wii:
                    throw new InvalidOperationException("Cannot obtain encoding definition for Wii MT Tex.");
            }

            return definition;
        }
    }

    enum MtTexPlatform
    {
        Wii,
        N3DS,
        Switch,

        PS3,

        Mobile,
        Pc
    }

    class MtTex_YCbCrColorShader : IColorShader
    {
        // https://en.wikipedia.org/wiki/YCbCr#JPEG_conversion
        private const int CbCrThreshold_ = 123; // usually 128, but 123 seems to work better here

        public Color Read(Color c)
        {
            var (a, y, cb, cr) = (c.G, c.A, c.B - CbCrThreshold_, c.R - CbCrThreshold_);
            return Color.FromArgb(a,
                Clamp(y + 1.402 * cr),
                Clamp(y - 0.344136 * cb - 0.714136 * cr),
                Clamp(y + 1.772 * cb));
        }

        public Color Write(Color c)
        {
            var (a, y, cb, cr) = (c.A,
                0.299 * c.R + 0.587 * c.G + 0.114 * c.B,
                CbCrThreshold_ - 0.168736 * c.R - 0.331264 * c.G + 0.5 * c.B,
                CbCrThreshold_ + 0.5 * c.R - 0.418688 * c.G - 0.081312 * c.B);
            return Color.FromArgb(Clamp(y), Clamp(cr), a, Clamp(cb));
        }

        private int Clamp(double n) => (int)Math.Max(0, Math.Min(n, 255));
    }

    class MtTex_NoAlphaShader : IColorShader
    {
        public Color Read(Color c)
        {
            return Color.FromArgb(255, c.R, c.G, c.B);
        }

        public Color Write(Color c)
        {
            return c;
        }
    }
}
