using Kanvas.Contract.DataClasses;
using Kanvas.Contract.Encoding;
using Kanvas.Encoding.BlockCompression.Pvr;
using Kanvas.Swizzle;
using PVRTexLib;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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
        public IEnumerable<Rgba32> Load(byte[] tex, EncodingOptions options)
        {
            // Initialize PVR Texture
            PVRTexture? texture = PvrTextureWrapper.CreateTexture(tex, (PVRTexLibPixelFormat)_format, options.Size);
            if (texture is null)
                throw new InvalidOperationException("Creating texture with PVRTexLib was not successful.");

            // Transcode texture to RGBA8888
            bool successful = texture.Transcode(PvrTextureWrapper.RGBA8888, PVRTexLibVariableType.UnsignedByteNorm, PVRTexLibColourSpace.Linear, PVRTexLibCompressorQuality.PVRTCHigh);
            if (!successful)
                throw new InvalidOperationException("Transcoding with PVRTexLib was not successful.");

            // Yield colors
            // Get colors in unswizzled order, so the framework applies the swizzle correctly
            var paddedWidth = GetPaddedWidth(options.Size.Width);
            var swizzle = GetSwizzle(options.Size.Width);

            var textureData = PvrTextureWrapper.GetData(texture);
            for (var y = 0; y < options.Size.Height; y++)
                for (var x = 0; x < options.Size.Width; x++)
                {
                    var sourcePoint = swizzle.Get(y * paddedWidth + x);
                    var textureIndex = (sourcePoint.Y * paddedWidth + sourcePoint.X) * 4;

                    yield return new Rgba32(textureData[textureIndex], textureData[textureIndex + 1], textureData[textureIndex + 2], textureData[textureIndex + 3]);
                }
        }

        /// <inheritdoc cref="Save"/>
        public byte[] Save(IEnumerable<Rgba32> colors, EncodingOptions options)
        {
            // Get colors in unswizzled order, so the framework applies the swizzle correctly
            var paddedWidth = GetPaddedWidth(options.Size.Width);
            var swizzle = GetSwizzle(options.Size.Width);

            var colorData = new byte[options.Size.Width * options.Size.Height * 4];

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
            PVRTexture? texture = PvrTextureWrapper.CreateTexture(colorData, PvrTextureWrapper.RGBA8888, options.Size);
            if (texture is null)
                throw new InvalidOperationException("Creating texture with PVRTexLib was not successful.");

            // Transcode texture to PVRTC
            texture.Transcode((ulong)_format, PVRTexLibVariableType.UnsignedByteNorm, PVRTexLibColourSpace.Linear, PVRTexLibCompressorQuality.PVRTCHigh);

            return PvrTextureWrapper.GetData(texture);
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
                new MasterSwizzle(paddedWidth, Point.Empty, [(1, 0), (2, 0), (0, 1), (0, 2)]) :
                new MasterSwizzle(paddedWidth, Point.Empty, [(1, 0), (2, 0), (4, 0), (0, 1), (0, 2)]);
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
