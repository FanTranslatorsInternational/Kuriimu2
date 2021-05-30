using System.Collections.Generic;
using Kanvas;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;

namespace plugin_spike_chunsoft.Images
{
    class CteHeader
    {
        [FixedLength(4)]
        public string magic;
        public int format;
        public int width;
        public int height;

        public int format2;
        public int zero1;
        public int dataOffset;
    }

    class CteSupport
    {
        private static readonly IDictionary<int, IColorEncoding> Formats = new Dictionary<int, IColorEncoding>
        {
            [8] = ImageFormats.La44()
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Formats);

            return definition;
        }
    }
}
