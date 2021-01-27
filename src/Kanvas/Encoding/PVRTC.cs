﻿using System;
using System.Collections.Generic;
using System.Drawing;
using Kanvas.Native;
using Kontract.Kanvas;

namespace Kanvas.Encoding
{
    /// <summary>
    /// Defines the PVRTC encoding.
    /// </summary>
    public class PVRTC : IColorEncoding
    {
        private readonly PvrtcFormat _format;
        private readonly int _width;
        private readonly int _height;

        /// <inheritdoc cref="IColorEncoding.BitDepth"/>
        public int BitDepth { get; }

        public int BitsPerValue { get; }

        public int ColorsPerValue => 16;

        /// <inheritdoc cref="IColorEncoding.FormatName"/>
        public string FormatName { get; }

        public PVRTC(PvrtcFormat format, int width, int height)
        {
            BitDepth = BitsPerValue = format == PvrtcFormat.PVRTCI_2bpp_RGB || format == PvrtcFormat.PVRTCI_2bpp_RGBA || format == PvrtcFormat.PVRTCII_2bpp ? 32 : 64;

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
            var successful = pvrTexture.Transcode(PixelFormat.RGBA8888, ChannelType.UnsignedByteNorm, ColorSpace.Linear, CompressionQuality.PVRTCHigh);
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
