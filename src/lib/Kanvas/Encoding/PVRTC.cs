using System;
using System.Collections.Generic;
using System.Drawing;
using Kanvas.Native;
using Kanvas.Swizzle;
using Kontract.Kanvas.Interfaces;
using Kontract.Kanvas.Models;

namespace Kanvas.Encoding
{
    /// <summary>
    /// Defines the PVRTC encoding.
    /// </summary>
    public class PVRTC : IColorEncoding
    {
        private readonly PvrtcFormat _format;

        /// <inheritdoc cref="BitDepth"/>
        public int BitDepth { get; }

        /// <inheritdoc cref="BitsPerValue"/>
        public int BitsPerValue { get; }

        /// <inheritdoc cref="ColorsPerValue"/>
        public int ColorsPerValue { get; }

        /// <inheritdoc cref="FormatName"/>
        public string FormatName { get; }

        public PVRTC(PvrtcFormat format)
        {
            BitDepth = BitsPerValue = format == PvrtcFormat.PVRTCI_2bpp_RGB || format == PvrtcFormat.PVRTCI_2bpp_RGBA || format == PvrtcFormat.PVRTCII_2bpp ? 2 : 4;
            ColorsPerValue = format == PvrtcFormat.PVRTCI_2bpp_RGB || format == PvrtcFormat.PVRTCI_2bpp_RGBA || format == PvrtcFormat.PVRTCII_2bpp ? 32 : 16;
            BitsPerValue = 64;

            _format = format;

            FormatName = format.ToString();
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
            // Get colors in unswizzled order, so the framework applies the swizzle correctly
            var paddedWidth = GetPaddedWidth(loadContext.Size.Width);
            var swizzle = GetSwizzle(loadContext.Size.Width);

            var textureData = pvrTexture.GetData();
            for (var y = 0; y < loadContext.Size.Height; y++)
                for (var x = 0; x < loadContext.Size.Width; x++)
                {
                    var sourcePoint = swizzle.Get(y * paddedWidth + x);
                    var textureIndex = (sourcePoint.Y * paddedWidth + sourcePoint.X) * 4;

                    yield return Color.FromArgb(textureData[textureIndex + 3], textureData[textureIndex], textureData[textureIndex + 1], textureData[textureIndex + 2]);
                }
        }

        /// <inheritdoc cref="Save"/>
        public byte[] Save(IEnumerable<Color> colors, EncodingSaveContext saveContext)
        {
            // Get colors in unswizzled order, so the framework applies the swizzle correctly
            var paddedWidth = GetPaddedWidth(saveContext.Size.Width);
            var swizzle = GetSwizzle(saveContext.Size.Width);

            var colorData = new byte[saveContext.Size.Width * saveContext.Size.Height * 4];

            var index = 0;
            foreach (var color in colors)
            {
                var targetPoint = swizzle.Get(index / 4);
                var textureIndex = (targetPoint.Y * paddedWidth + targetPoint.X) * 4;

                colorData[textureIndex] = color.R;
                colorData[textureIndex + 1] = color.G;
                colorData[textureIndex + 2] = color.B;
                colorData[textureIndex + 3] = color.A;

                index += 4;
            }

            // Initialize PVR Texture
            var pvrTexture = PvrTexture.Create(colorData, (uint)saveContext.Size.Width, (uint)saveContext.Size.Height, 1, PixelFormat.RGBA8888, ChannelType.UnsignedByteNorm, ColorSpace.Linear);

            // Transcode texture to PVRTC
            pvrTexture.Transcode((PixelFormat)_format, ChannelType.UnsignedByteNorm, ColorSpace.Linear, CompressionQuality.PVRTCHigh);

            return pvrTexture.GetData();
        }

        private int GetPaddedWidth(int width)
        {
            var padFactor = BitDepth == 4 ? 3 : 7;
            return (width + padFactor) & ~padFactor;
        }

        private MasterSwizzle GetSwizzle(int width)
        {
            var paddedWidth = GetPaddedWidth(width);

            return BitDepth == 4 ?
                new MasterSwizzle(paddedWidth, Point.Empty, new[] { (1, 0), (2, 0), (0, 1), (0, 2) }) :
                new MasterSwizzle(paddedWidth, Point.Empty, new[] { (1, 0), (2, 0), (4, 0), (0, 1), (0, 2) });
        }
    }

    public enum PvrtcFormat : ulong
    {
        PVRTCI_2bpp_RGB,
        PVRTCI_2bpp_RGBA,
        PVRTCI_4bpp_RGB,
        PVRTCI_4bpp_RGBA,
        PVRTCII_2bpp,
        PVRTCII_4bpp
    }
}
