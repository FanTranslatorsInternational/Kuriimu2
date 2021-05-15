using System.Collections.Generic;
using Kanvas;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;

namespace plugin_nintendo.Images
{
    class SmdhHeader
    {
        [FixedLength(4)]
        public string magic;
        public short version;
        public short reserved;
    }

    class SmdhApplicationTitle
    {
        [FixedLength(0x80, StringEncoding = StringEncoding.Unicode)]
        public string shortDesc;
        [FixedLength(0x100, StringEncoding = StringEncoding.Unicode)]
        public string longDesc;
        [FixedLength(0x80, StringEncoding = StringEncoding.Unicode)]
        public string publisher;
    }

    class SmdhAppSettings
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
