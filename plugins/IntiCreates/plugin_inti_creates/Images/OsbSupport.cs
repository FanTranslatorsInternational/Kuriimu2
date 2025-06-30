using Kanvas;
using Kanvas.Contract.Encoding;
using Kanvas.Encoding;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Extensions;
using Konnect.Plugin.File.Image;

namespace plugin_inti_creates.Images
{
    class OsbHeader
    {
        public int nodeOffset;
        public int dataSize;
        public int format;
        public int width;
        public int height;
        public int dataOffset;

        public int postSize;
        public int postOffset;

        public int unk1;
        public int unk2;
        public int unk3;
    }

    enum Platform
    {
        N3DS,
        PC
    }

    class OsbSupport
    {
        public static readonly IDictionary<int, IColorEncoding> Formats = new Dictionary<int, IColorEncoding>
        {
            [0] = new Rgba(8, 8, 8, 8, "ARGB"),
            [4] = ImageFormats.Rgba4444()
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Formats);

            return definition;
        }

        public static Platform DeterminePlatform(UPath fileName)
        {
            switch (fileName.GetExtensionWithDot())
            {
                case ".osbctr":
                    return Platform.N3DS;

                default:
                    return Platform.PC;
            }
        }
    }
}
