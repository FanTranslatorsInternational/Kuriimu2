﻿using System.Collections.Generic;
using System.Drawing;
using Kanvas;
using Kanvas.Encoding;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;

namespace plugin_dotemu.Images
{
    class XnbHeader
    {
        [FixedLength(4)]
        public string magic;

        public byte major;
        public byte minor;
        public int fileSize;
        public byte itemCount;

        public string className;
        public int unk1;
        public short unk2;

        public int format;
        public int width;
        public int height;
        public int mipCount;
        public int dataSize;
    }

    class PremultiplyAlphaShader : IColorShader
    {
        public Color Read(Color c)
        {
            return c;
        }

        public Color Write(Color c)
        {
            return Color.FromArgb(
                c.A,
                PreMultiply(c.R, c.A),
                PreMultiply(c.G, c.A),
                PreMultiply(c.B, c.A));
        }

        private byte PreMultiply(byte a, byte b) => (byte)(a * b / 255);
    }

    class XnbSupport
    {
        public static readonly IDictionary<int, IColorEncoding> Formats = new Dictionary<int, IColorEncoding>
        {
            [0] = new Rgba(8, 8, 8, 8, "ABGR"),
            [4] = ImageFormats.Dxt1(),
            [5] = ImageFormats.Dxt3(),
            [6] = ImageFormats.Dxt5()
        };

        public static readonly IDictionary<int,IColorShader> Shaders=new Dictionary<int, IColorShader>
        {
            [0] = new PremultiplyAlphaShader(),
            [4] = new PremultiplyAlphaShader(),
            [5] = new PremultiplyAlphaShader(),
            [6] = new PremultiplyAlphaShader()
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Formats);
            definition.AddColorShaders(Shaders);

            return definition;
        }
    }
}
