using Kanvas;
using Konnect.Plugin.File.Image;
using Komponent.Contract.Enums;

namespace plugin_nintendo.Images
{
    struct BnrHeader
    {
        public short version;
        public ushort crc16_v1;
        public ushort crc16_v2;
        public ushort crc16_v3;
        public ushort crc16_v103;
    }

    class BnrSupport
    {
        public static EncodingDefinition GetEncodingDefinition()
        {
            var encodingDefinition = new EncodingDefinition();
            encodingDefinition.AddPaletteEncoding(0, ImageFormats.Rgb555());
            encodingDefinition.AddIndexEncoding(0, ImageFormats.I4(BitOrder.LeastSignificantBitFirst), [0]);

            return encodingDefinition;
        }
    }
}
