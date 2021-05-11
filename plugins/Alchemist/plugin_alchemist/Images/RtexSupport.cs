using System.Collections.Generic;
using Kanvas;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_alchemist.Images
{
    class RtexHeader
    {
        [FixedLength(4)]
        public string magic = "RTEX";

        public int zero0;
        public short width;
        public short height;
        public short paddedWidth;
        public short paddedHeight;

        public int dataOffset;
        public int dataSize;

        public byte format;
        public byte unk1;
        public short unk2;
        public int unk3;
    }

    [Endianness(ByteOrder = ByteOrder.BigEndian)]
    class RtexDataHeader
    {
        [FixedLength(2)]
        public string magic = "RZ";
        public int decompSize;

    }

    class RtexSupport
    {
        private static readonly IDictionary<int, IColorEncoding> Formats = new Dictionary<int, IColorEncoding>
        {
            [0x04] = ImageFormats.Rgba8888(),
            [0x34] = ImageFormats.La44()
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Formats);

            return definition;
        }
    }
}
