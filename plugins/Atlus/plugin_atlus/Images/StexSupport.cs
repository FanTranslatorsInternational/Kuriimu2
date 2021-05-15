using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas;
using Kanvas.Encoding;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_atlus.Images
{
    class StexHeader
    {
        [FixedLength(4)]
        public string magic = "STEX";
        public uint zero0;
        public uint const0 = 0xDE1;
        public int width;
        public int height;
        public uint dataType;
        public uint imageFormat;
        public int dataSize;
    }

    class StexEntry
    {
        public int offset;
        public int unk1;
    }

    class StexImageInfo : ImageInfo
    {
        public StexEntry Entry { get; }

        public StexImageInfo(byte[] imageData, int imageFormat, Size imageSize, StexEntry entry) : base(imageData, imageFormat, imageSize)
        {
            Entry = entry;
        }

        public StexImageInfo(byte[] imageData, IList<byte[]> mipMaps, int imageFormat, Size imageSize, StexEntry entry) : base(imageData, mipMaps, imageFormat, imageSize)
        {
            Entry = entry;
        }
    }

    class StexSupport
    {
        private static readonly IDictionary<uint, IColorEncoding> Formats = new Dictionary<uint, IColorEncoding>
        {
            //composed of dataType and PixelFormat
            //short+short
            [0x14016752] = ImageFormats.Rgba8888(),
            [0x80336752] = ImageFormats.Rgba4444(),
            [0x80346752] = ImageFormats.Rgba5551(),
            [0x14016754] = ImageFormats.Rgb888(),
            [0x83636754] = ImageFormats.Rgb565(),
            [0x14016756] = ImageFormats.A8(),
            [0x67616756] = ImageFormats.A4(BitOrder.LeastSignificantBitFirst),
            [0x14016757] = ImageFormats.L8(),
            [0x67616757] = ImageFormats.L4(BitOrder.LeastSignificantBitFirst),
            [0x14016758] = ImageFormats.La88(),
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
