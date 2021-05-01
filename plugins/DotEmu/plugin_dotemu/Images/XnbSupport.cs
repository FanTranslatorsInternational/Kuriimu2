using System.Collections.Generic;
using Kanvas;
using Kanvas.Encoding;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;

namespace plugin_dotemu.Images
{
    class XnbHeader
    {
        [FixedLength(4)]
        public string magic;

        public byte major;
        public byte minor;
        public int fileSize;
        public byte itemCount;

        public string className;
        public int unk1;
        public short unk2;

        public int format;
        public int width;
        public int height;
        public int mipCount;
        public int dataSize;
    }

    class XnbSupport
    {
        public static readonly IDictionary<int, IColorEncoding> Formats = new Dictionary<int, IColorEncoding>
        {
            [0] = new Rgba(8, 8, 8, 8, "ARGB"),
            [4] = ImageFormats.Dxt1(),
            [5] = ImageFormats.Dxt3(),
            [6] = ImageFormats.Dxt5()
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Formats);

            return definition;
        }
    }
}
