using Kanvas.Contract.Encoding;
using Kanvas.Encoding;
using Komponent.Contract.Aspects;
using Komponent.Contract.Enums;
using Konnect.Plugin.File.Image;

namespace plugin_nintendo.Images
{
    [Alignment(0x20)]
    class GcBnrHeader
    {
        [FixedLength(4)]
        public string magic; // BNR1 or BNR2
    }

    class GcBnrTitleInfo
    {
        [FixedLength(0x20, StringEncoding = StringEncoding.Sjis)]
        public string gameName;
        [FixedLength(0x20, StringEncoding = StringEncoding.Sjis)]
        public string company;
        [FixedLength(0x40, StringEncoding = StringEncoding.Sjis)]
        public string fullGameName;
        [FixedLength(0x40, StringEncoding = StringEncoding.Sjis)]
        public string fullCompany;
        [FixedLength(0x80, StringEncoding = StringEncoding.Sjis)]
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
