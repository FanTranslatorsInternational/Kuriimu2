using System.Collections.Generic;
using System.Linq;
using Kanvas;
using Kanvas.Encoding;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;

namespace most_wanted_ent.Images
{
    class CtgdSection
    {
        [FixedLength(8)]
        public string magic;
        public int size;

        [VariableLength("size", Offset = -0xC)]
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
            definition.AddIndexEncodings(IndexFormats.Select(x => (x.Key, new IndexEncodingDefinition(x.Value, new[] { 0 }))).ToArray());

            return definition;
        }
    }
}
