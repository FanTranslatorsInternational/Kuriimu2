using Konnect.Plugin.File.Image;
using plugin_nintendo.NW4C;

namespace plugin_nintendo.Images
{
    class BclimHeader
    {
        public short width;
        public short height;
        public byte format;
        public byte transformation;
        public short alignment;
        public int dataSize;
    }

    class BflimHeader
    {
        public short width;
        public short height;
        public short alignment;
        public byte format;
        public byte swizzleTileMode;
        public int dataSize;
    }

    class BxlimSupport
    {
        public static EncodingDefinition GetCtrDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Nw4cImageFormats.CtrFormats);

            return definition;
        }

        public static EncodingDefinition GetCafeDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Nw4cImageFormats.CafeFormats);

            return definition;
        }
    }
}
