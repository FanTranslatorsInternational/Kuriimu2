using Kanvas;
using Kanvas.Contract.Encoding;
using Komponent.Contract.Enums;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Plugin.File.Image;

namespace plugin_sting_entertainment.Images
{
    class TexHeader
    {
        public int unk1;
        public int dataSize;
        public int width;
        public int height;
    }

    class TexSupport
    {
        private static readonly IDictionary<int, IIndexEncoding> IndexFormats = new Dictionary<int, IIndexEncoding>
        {
            [0] = ImageFormats.I8()
        };

        private static readonly IDictionary<int, IColorEncoding> PaletteFormats = new Dictionary<int, IColorEncoding>
        {
            [0] = ImageFormats.Rgba8888(ByteOrder.BigEndian)
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();

            definition.AddPaletteEncodings(PaletteFormats);
            definition.AddIndexEncodings(IndexFormats.Select(x => (x.Key, new IndexEncodingDefinition
            {
                IndexEncoding = x.Value,
                PaletteEncodingIndices = [0]
            })).ToArray());

            return definition;
        }
    }
}
