using Kanvas.Contract.DataClasses;
using Kanvas.Contract.Encoding;
using SixLabors.ImageSharp.PixelFormats;

namespace Kanvas.Encoding.PlatformSpecific.Wii
{
    internal class Rgba8 : IColorEncoding
    {
        public int BitDepth => 32;
        public ColorChannelBitDepths ColorChannelBitDepths => new(8, 8, 8, 8);
        public int BitsPerValue => 512;
        public int ColorsPerValue => 16;
        public string FormatName => "RGBA8_Wii";

        public IEnumerable<Rgba32> Load(byte[] input, EncodingOptions options)
        {
            for (var i = 0; i < input.Length; i += 64)
            {
                for (var j = 0; j < 32; j += 2)
                    yield return new Rgba32(input[i + j + 1], input[i + 32 + j], input[i + 32 + j + 1], input[i + j]);
            }
        }

        public byte[] Save(IEnumerable<Rgba32> colors, EncodingOptions options)
        {
            var buffer = new byte[options.Size.Width * options.Size.Height * 4];

            var index = 0;
            foreach (var color in colors)
            {
                for (var i = 0; i < 16; i++)
                {
                    buffer[index + i] = color.A;
                    buffer[index + i + 1] = color.R;
                    buffer[index + i + 32] = color.G;
                    buffer[index + i + 31 + 1] = color.B;
                }

                index += 64;
            }

            return buffer;
        }
    }
}
