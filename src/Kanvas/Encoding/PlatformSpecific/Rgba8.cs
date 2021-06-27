using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Kontract.Kanvas;
using Kontract.Kanvas.Model;

namespace Kanvas.Encoding.PlatformSpecific
{
    class Rgba8 : IColorEncoding
    {
        public int BitDepth => 32;
        public int BitsPerValue => 512;
        public int ColorsPerValue => 16;
        public string FormatName => "RGBA8_Wii";

        public IEnumerable<Color> Load(byte[] input, EncodingLoadContext loadContext)
        {
            for (var i = 0; i < input.Length; i += 64)
            {
                for (var j = 0; j < 32; j += 2)
                    yield return Color.FromArgb(input[i + j], input[i + j + 1], input[i + 32 + j], input[i + 32 + j + 1]);
            }
        }

        public byte[] Save(IEnumerable<Color> colors, EncodingSaveContext saveContext)
        {
            var buffer = new byte[saveContext.Size.Width * saveContext.Size.Height * 4];

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
