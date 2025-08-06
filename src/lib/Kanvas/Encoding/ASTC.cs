using Kanvas.Contract.DataClasses;
using Kanvas.Contract.Encoding;
using Kanvas.Encoding.BlockCompression.Pvr;
using PVRTexLib;
using SixLabors.ImageSharp.PixelFormats;

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
            var textureData = PvrTextureWrapper.GetData(texture);
            for (var i = 0L; i < textureData.Length; i += 4)
                yield return new Rgba32(textureData[i], textureData[i + 1], textureData[i + 2], textureData[i + 3]);
        }

        /// <inheritdoc cref="Save"/>
        public byte[] Save(IEnumerable<Rgba32> colors, EncodingOptions options)
        {
            var colorData = new byte[options.Size.Width * options.Size.Height * 4];

            var index = 0;
            foreach (var color in colors)
            {
                colorData[index++] = color.R;
                colorData[index++] = color.G;
                colorData[index++] = color.B;
                colorData[index++] = color.A;
            }

            // Initialize PVR Texture
            PVRTexture? texture = PvrTextureWrapper.CreateTexture(colorData, PvrTextureWrapper.RGBA8888, options.Size);
            if (texture is null)
                throw new InvalidOperationException("Creating texture with PVRTexLib was not successful.");

            // Transcode texture to PVRTC
            texture.Transcode((ulong)_format, PVRTexLibVariableType.UnsignedByteNorm, PVRTexLibColourSpace.Linear, PVRTexLibCompressorQuality.PVRTCHigh);

            return PvrTextureWrapper.GetData(texture);
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
