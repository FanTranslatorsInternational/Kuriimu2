using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas.Encoding;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;

namespace plugin_atlus.Images
{
    class StexHeader
    {
        [FixedLength(4)] 
        public string magic = "STEX";
        public uint zero0;
        public uint const0=0xDE1;
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

        public StexImageInfo(byte[] imageData, int imageFormat, Size imageSize,StexEntry entry) : base(imageData, imageFormat, imageSize)
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
            [0x14016752] = new Rgba(8, 8, 8, 8),
            [0x80336752] = new Rgba(4, 4, 4, 4),
            [0x80346752] = new Rgba(5, 5, 5, 1),
            [0x14016754] = new Rgba(8, 8, 8),
            [0x83636754] = new Rgba(5, 6, 5),
            [0x14016756] = new La(0, 8),
            [0x67616756] = new La(0, 4),
            [0x14016757] = new La(8, 0),
            [0x67616757] = new La(4, 0),
            [0x14016758] = new La(8, 8),
            [0x67606758] = new La(4, 4),
            [0x0000675A] = new Etc1(false, true),
            [0x0000675B] = new Etc1(true, true),
            [0x1401675A] = new Etc1(false, true),
            [0x1401675B] = new Etc1(true, true)
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Formats.ToDictionary(x => (int)x.Key, y => y.Value));

            return definition;
        }
    }
}
