using Kanvas;
using Kanvas.Contract.Encoding;
using Konnect.Plugin.File.Image;

namespace plugin_spike_chunsoft.Images
{
    class CteHeader
    {
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
        public static readonly IDictionary<int, IColorEncoding> Formats = new Dictionary<int, IColorEncoding>
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
