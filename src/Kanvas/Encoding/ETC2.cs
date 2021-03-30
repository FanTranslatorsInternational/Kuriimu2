using System;
using System.Collections.Generic;
using System.Drawing;
using Kanvas.Native;
using Kontract.Kanvas;
using Kontract.Kanvas.Model;

namespace Kanvas.Encoding
{
    // TODO: Unswizzle for framework support
    public class Etc2 : IColorEncoding
    {
        private readonly Etc2Format _format;

        /// <inheritdoc cref="BitDepth"/>
        public int BitDepth { get; }

        /// <inheritdoc cref="BitsPerValue"/>
        public int BitsPerValue { get; }

        /// <inheritdoc cref="ColorsPerValue"/>
        public int ColorsPerValue => 16;

        /// <inheritdoc cref="FormatName"/>
        public string FormatName { get; }

        public Etc2(Etc2Format format)
        {
            _format = format;

            BitDepth = _format == Etc2Format.EAC_RG11 || _format == Etc2Format.RGBA ? 8 : 4;
            BitsPerValue = _format == Etc2Format.EAC_RG11 || _format == Etc2Format.RGBA ? 128 : 64;

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

            // Transcode texture to ETC2
            pvrTexture.Transcode((PixelFormat)_format, ChannelType.UnsignedByteNorm, ColorSpace.Linear, CompressionQuality.PVRTCHigh);

            return pvrTexture.GetData();
        }
    }

    public enum Etc2Format
    {
        RGB = 22,
        RGBA,
        RGB_A1,
        EAC_R11,
        EAC_RG11
    }
}
