using Kanvas;
using Kanvas.Contract.Encoding;
using Komponent.Contract.Aspects;
using Komponent.Contract.Enums;
using Konnect.Plugin.File.Image;

namespace plugin_nintendo.Images
{
    struct SmdhHeader
    {
        public string magic;
        public short version;
        public short reserved;
    }

    struct SmdhApplicationTitle
    {
        public string shortDesc;
        public string longDesc;
        public string publisher;
    }

    struct SmdhAppSettings
    {
        [FixedLength(0x10)]
        public byte[] gameRating;
        public int regionLockout;
        public int makerID;
        public long makerBITID;
        public int flags;
        public byte eulaVerMinor;
        public byte eulaVerMajor;
        public short reserved;
        public int animDefaultFrame;
        public int streetPassID;
    }

    class SmdhSupport
    {
        private static readonly IDictionary<int, IColorEncoding> SmdhFormats = new Dictionary<int, IColorEncoding>
        {
            [0] = ImageFormats.Rgb565()
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(SmdhFormats);

            return definition;
        }
    }
}
