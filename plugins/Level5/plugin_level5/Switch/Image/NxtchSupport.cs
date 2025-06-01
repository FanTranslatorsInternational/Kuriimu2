using Kanvas;
using Konnect.Plugin.File.Image;

namespace plugin_level5.Switch.Image
{
    struct NxtchHeader
    {
        public string magic; // NXTCH000
        public int textureDataSize;
        public int unk1;
        public int unk2;
        public int width;
        public int height;
        public int unk3;
        public int unk4;
        public int format;
        public int mipMapCount;
        public int textureDataSize2;
    }

    class NxtchSupport
    {
        public static EncodingDefinition GetEncodingDefinition()
        {
            var encodingDefinition = new EncodingDefinition();
            encodingDefinition.AddColorEncoding(0x25, ImageFormats.Rgba8888());
            encodingDefinition.AddColorEncoding(0x42, ImageFormats.Dxt1());
            encodingDefinition.AddColorEncoding(0x4D, ImageFormats.Bc7());

            return encodingDefinition;
        }
    }
}
