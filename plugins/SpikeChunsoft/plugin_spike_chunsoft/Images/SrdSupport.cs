using System.Collections.Generic;
using System.Drawing;
using Kanvas;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_spike_chunsoft.Images
{
    [Endianness(ByteOrder = ByteOrder.BigEndian)]
    class SrdHeader
    {
        [FixedLength(4)]
        public string magic;
        public int sectionSize;
        public int subDataSize;
        public int unk1;
    }

    [Alignment(0x10)]
    class SrdSection
    {
        public SrdHeader header;

        [VariableLength("header.sectionSize")]
        public byte[] sectionData;

        [VariableLength("header.subDataSize")]
        public byte[] subData;
    }

    class SrdImageInfo : ImageInfo
    {
        public SrdSection Section { get; }

        public SrdImageInfo(byte[] imageData, int imageFormat, Size imageSize, SrdSection section) : base(imageData, imageFormat, imageSize)
        {
            Section = section;
        }

        public SrdImageInfo(byte[] imageData, IList<byte[]> mipMaps, int imageFormat, Size imageSize, SrdSection section) : base(imageData, mipMaps, imageFormat, imageSize)
        {
            Section = section;
        }
    }

    class SrdSupport
    {
        public static readonly IDictionary<int, IColorEncoding> Formats = new Dictionary<int, IColorEncoding>
        {
            [0x0F] = ImageFormats.Dxt1(),
            [0x11] = ImageFormats.Dxt5(),
            [0x14] = ImageFormats.Ati2(),
            [0x16] = ImageFormats.Ati1(),
            [0x1C] = ImageFormats.Bc7()
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Formats);

            return definition;
        }
    }
}
