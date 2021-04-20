using System.Collections.Generic;
using System.Drawing;
using Kanvas;
using Kanvas.Encoding;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;

namespace plugin_bandai_namco.Images
{
    class NstpHeader
    {
        [FixedLength(4)]
        public string magic;
        public int version = 0x00010000;
        public int imgCount;
        public int hashOffset;  // Hashes are CRC32B

        [FixedLength(0x10)] 
        public byte[] padding = new byte[0x10];
    }

    class NstpImageHeader
    {
        public int nameOffset;
        public int dataSize;
        public int dataOffset;
        public int format;

        public short width;
        public short height;
        public int unk1;
        public int unk2;
        public int unk3;
    }

    class NstpImageInfo : ImageInfo
    {
        public NstpImageHeader Entry { get; }

        public NstpImageInfo(byte[] imageData, int imageFormat, Size imageSize, NstpImageHeader entry) : base(imageData, imageFormat, imageSize)
        {
            Entry = entry;
        }
    }

    class NstpSupport
    {
        private static readonly IDictionary<int, IColorEncoding> NstpFormats = new Dictionary<int, IColorEncoding>
        {
            [0x01] = ImageFormats.L8(),

            [0x04] = new Rgba(5, 5, 5, 1, "ARGB"),
            [0x05] = new Rgba(4, 4, 4, 4, "ARGB"),
            [0x06] = new Rgba(8, 8, 8, 8, "ARGB"),
            [0x07] = new Rgba(8, 8, 8, 8, "ABGR")
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(NstpFormats);

            return definition;
        }
    }
}
