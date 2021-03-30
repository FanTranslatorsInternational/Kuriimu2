using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.Native;
using Kontract.Kanvas;
using Kontract.Kanvas.Model;

namespace Kanvas.Encoding
{
    public class Astc : IColorEncoding
    {
        private readonly AstcFormat _format;

        /// <inheritdoc cref="BitDepth"/>
        public int BitDepth { get; }

        /// <inheritdoc cref="BitsPerValue"/>
        public int BitsPerValue { get; }

        /// <inheritdoc cref="ColorsPerValue"/>
        public int ColorsPerValue { get; }

        /// <inheritdoc cref="FormatName"/>
        public string FormatName { get; }

        public Astc(AstcFormat format)
        {
            _format = format;

            BitDepth = -1;
            BitsPerValue = 128;
            ColorsPerValue = format.ToString()[5..].Split('x').Aggregate(1, (a, b) => a * int.Parse(b));

            FormatName = format.ToString().Replace("_", " ");
        }

        /// <inheritdoc cref="Load"/>
        public IEnumerable<Color> Load(byte[] tex, EncodingLoadContext loadContext)
        {
            // Initialize PVR Texture
            var pvrTexture = PvrTexture.Create(tex, (uint)loadContext.Size.Width, (uint)loadContext.Size.Height, 1, (PixelFormat)_format, ChannelType.UnsignedByte, ColorSpace.Linear);

            // Transcode texture to RGBA8888
            var successful = pvrTexture.Transcode(PixelFormat.RGBA8888, ChannelType.UnsignedByteNorm, ColorSpace.Linear, CompressionQuality.PVRTCHigh);
            if (!successful)
                throw new InvalidOperationException("Transcoding with PVRTexLib was not successful.");

            // Yield colors
            var textureData = pvrTexture.GetData();
            for (var i = 0L; i < textureData.Length; i += 4)
                yield return Color.FromArgb(textureData[i + 3], textureData[i], textureData[i + 1], textureData[i + 2]);
        }

        /// <inheritdoc cref="Save"/>
        public byte[] Save(IEnumerable<Color> colors, EncodingSaveContext saveContext)
        {
            var colorData = new byte[saveContext.Size.Width * saveContext.Size.Height * 4];

            var index = 0;
            foreach (var color in colors)
            {
                colorData[index++] = color.R;
                colorData[index++] = color.G;
                colorData[index++] = color.B;
                colorData[index++] = color.A;
            }

            // Initialize PVR Texture
            var pvrTexture = PvrTexture.Create(colorData, (uint)saveContext.Size.Width, (uint)saveContext.Size.Height, 1, PixelFormat.RGBA8888, ChannelType.UnsignedByteNorm, ColorSpace.Linear);

            // Transcode texture to ASTC
            pvrTexture.Transcode((PixelFormat)_format, ChannelType.UnsignedByteNorm, ColorSpace.Linear, CompressionQuality.PVRTCHigh);

            return pvrTexture.GetData();
        }
    }

    public enum AstcFormat
    {
        ASTC_4x4 = 27,
        ASTC_5x4,
        ASTC_5x5,
        ASTC_6x5,
        ASTC_6x6,
        ASTC_8x5,
        ASTC_8x6,
        ASTC_8x8,
        ASTC_10x5,
        ASTC_10x6,
        ASTC_10x8,
        ASTC_10x10,
        ASTC_12x10,
        ASTC_12x12,

        ASTC_3x3x3,
        ASTC_4x3x3,
        ASTC_4x4x3,
        ASTC_4x4x4,
        ASTC_5x4x4,
        ASTC_5x5x4,
        ASTC_5x5x5,
        ASTC_6x5x5,
        ASTC_6x6x5,
        ASTC_6x6x6,
    }
}
