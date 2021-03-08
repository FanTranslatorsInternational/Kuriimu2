using System.Collections.Generic;
using System.Linq;
using Kanvas;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_kadokawa.Images
{
    [Alignment(0x20)]
    class CtxHeader
    {
        [FixedLength(8)]
        public string magic = "CTX 10 \0";
        public int width;
        public int height;
        public int width2;
        public int height2;
        public int unk1;
        public int format;
        public int unk2;
        public int dataSize;
    }

    class CtxSupport
    {
        private static readonly IDictionary<uint, IColorEncoding> Formats = new Dictionary<uint, IColorEncoding>
        {
            // Composed of dataType and PixelFormat
            // Short + short
            [0x14016752] = ImageFormats.Rgba8888(),
            [0x00006752] = ImageFormats.Rgba8888(),
            [0x80336752] = ImageFormats.Rgba4444(),
            [0x80346752] = ImageFormats.Rgba5551(),
            [0x14016754] = ImageFormats.Rgb888(),
            [0x83636754] = ImageFormats.Rgb565(),
            [0x14016756] = ImageFormats.A8(),
            [0x67616756] = ImageFormats.A4(BitOrder.LeastSignificantBitFirst),
            [0x14016757] = ImageFormats.L8(),
            [0x67616757] = ImageFormats.L4(BitOrder.LeastSignificantBitFirst),
            [0x14016757] = ImageFormats.La88(),
            [0x67606758] = ImageFormats.La44(),
            [0x0000675A] = ImageFormats.Etc1(true),
            [0x0000675B] = ImageFormats.Etc1A4(true),
            [0x1401675A] = ImageFormats.Etc1(true),
            [0x1401675B] = ImageFormats.Etc1A4(true)
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Formats.ToDictionary(x => (int)x.Key, y => y.Value));

            return definition;
        }
    }
}
