using Kanvas;
using Kanvas.Contract.Encoding;
using Kanvas.Encoding;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Plugin.File.Image;

namespace plugin_most_wanted_ent.Images
{
    class CtgdSection
    {
        public string magic;
        public int size;
        public byte[] data;
    }

    class CtgdSupport
    {
        private static readonly IDictionary<int, IIndexEncoding> IndexFormats = new Dictionary<int, IIndexEncoding>
        {
            [0] = ImageFormats.I8()
        };

        private static readonly IDictionary<int, IColorEncoding> PaletteFormats = new Dictionary<int, IColorEncoding>
        {
            [0] = new Rgba(5, 5, 5, "BGR")
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
