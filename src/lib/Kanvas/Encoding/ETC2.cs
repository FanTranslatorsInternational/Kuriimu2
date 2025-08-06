using Kanvas.Contract.DataClasses;
using Kanvas.Contract.Encoding;
using Kanvas.Encoding.BlockCompression.Pvr;
using PVRTexLib;
using SixLabors.ImageSharp.PixelFormats;

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

            BitDepth = _format == Etc2Format.EAC_RG11 || _format == Etc2Format.ETC2_RGBA ? 8 : 4;
            BitsPerValue = _format == Etc2Format.EAC_RG11 || _format == Etc2Format.ETC2_RGBA ? 128 : 64;

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

    public enum Etc2Format
    {
        ETC2_RGB = 22,
        ETC2_RGBA,
        ETC2_RGB_A1,
        EAC_R11,
        EAC_RG11
    }
}
