using System;
using System.Collections.Generic;
using System.Drawing;
using Kanvas.Native;
using Kontract.Kanvas;

namespace Kanvas.Encoding
{
    // TODO: Use PVRTexLibrary
    // TODO: Set properties
    /// <summary>
    /// Defines the ASTC encoding.
    /// </summary>
    public class ASTC : IColorEncodingKnownDimensions
    {
        private readonly AstcFormat _format;
        private readonly int _width;
        private readonly int _height;

        /// <inheritdoc cref="IColorEncoding.BitDepth"/>
        public int BitDepth { get; }

        public int BitsPerValue { get; }

        public int ColorsPerValue { get; }

        /// <summary>
        /// The number of bits one block contains of.
        /// </summary>
        public int BlockBitDepth { get; }

        /// <inheritdoc cref="IColorEncoding.FormatName"/>
        public string FormatName { get; }

        /// <inheritdoc cref="IColorEncoding.IsBlockCompression"/>
        public bool IsBlockCompression => true;

        /// <inheritdoc cref="IColorEncodingKnownDimensions.Width"/>
        public int Width { private get; set; } = -1;

        /// <inheritdoc cref="IColorEncodingKnownDimensions.Height"/>
        public int Height { private get; set; } = -1;

        public ASTC(AstcFormat format, int width, int height)
        {
            _format = format;
            _width = width;
            _height = height;

            FormatName = format.ToString();
        }

        public IEnumerable<Color> Load(byte[] tex, int taskCount)
        {
            // Initialize PVR Texture
            var pvrTexture = PvrTexture.Create(tex, (uint)_width, (uint)_height, 1, (PixelFormat)_format, ChannelType.UnsignedByte, ColorSpace.Linear);

            // Transcode texture to RGBA8888
            var successful = pvrTexture.Transcode(Native.PixelFormat.RGBA8888, ChannelType.UnsignedByteNorm, ColorSpace.Linear, CompressionQuality.PVRTCHigh);
            if (!successful)
                throw new InvalidOperationException("Transcoding with PVRTexLib was not successful.");

            // Yield colors
            var textureData = pvrTexture.GetData();
            for (var i = 0L; i < textureData.Length; i += 4)
                yield return Color.FromArgb(textureData[i + 3], textureData[i], textureData[i + 1], textureData[i + 2]);
        }

        public byte[] Save(IEnumerable<Color> colors, int taskCount)
        {
            var colorData = new byte[_width * _height * 4];

            var index = 0;
            foreach (var color in colors)
            {
                colorData[index++] = color.R;
                colorData[index++] = color.G;
                colorData[index++] = color.B;
                colorData[index++] = color.A;
            }

            // Initialize PVR Texture
            var pvrTexture = PvrTexture.Create(colorData, (uint)_width, (uint)_height, 1, PixelFormat.RGBA8888, ChannelType.UnsignedByteNorm, ColorSpace.Linear);

            // Transcode texture to PVRTC
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
