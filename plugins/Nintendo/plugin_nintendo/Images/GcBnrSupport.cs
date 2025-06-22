using Kanvas.Contract.Encoding;
using Kanvas.Encoding;
using Komponent.Contract.Enums;
using Konnect.Plugin.File.Image;

namespace plugin_nintendo.Images
{
    struct GcBnrHeader
    {
        public string magic; // BNR1 or BNR2
    }

    struct GcBnrTitleInfo
    {
        public string gameName;
        public string company;
        public string fullGameName;
        public string fullCompany;
        public string description;
    }

    class GcBnrSupport
    {
        private static Dictionary<int, IColorEncoding> Encodings = new()
        {
            [0] = new Rgba(5, 5, 5, 1, "ABGR", ByteOrder.BigEndian)
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var encodingDefinition = new EncodingDefinition();
            encodingDefinition.AddColorEncodings(Encodings);

            return encodingDefinition;
        }
    }
}
