using System.Collections.Generic;
using Kanvas.Encoding;
using Kontract.Kanvas;
using Kontract.Models.Image;

namespace plugin_nintendo.Images
{
    class RawJtexHeader
    {
        public uint dataOffset;
        public int format;
        public int width;
        public int height;
        public int paddedWidth;
        public int paddedHeight;
    }

    public class RawJtexSupport
    {
        private static readonly IDictionary<int, IColorEncoding> JtexFormats = new Dictionary<int, IColorEncoding>
        {
            [2] = new Rgba(8, 8, 8, 8),
            [3] = new Rgba(8, 8, 8),
            [4] = new Rgba(4, 4, 4, 4)
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(JtexFormats);

            return definition;
        }
    }
}
