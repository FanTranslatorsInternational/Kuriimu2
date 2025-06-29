using Kanvas;
using Kanvas.Contract.Encoding;
using Komponent.Contract.Aspects;
using Konnect.Plugin.File.Image;

namespace plugin_furyu.Images
{
    class RtexHeader
    {
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

    class RtexDataHeader
    {
        public string magic = "RZ";
        public int decompSize;

    }

    class RtexSupport
    {
        public static readonly IDictionary<int, IColorEncoding> Formats = new Dictionary<int, IColorEncoding>
        {
            [0x04] = ImageFormats.Rgba8888(),
            [0x1F] = ImageFormats.Rgb565(),
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
