using System.Collections.Generic;
using Kanvas;
using Kanvas.Encoding;
using Kontract.Kanvas;
using Kontract.Models.Image;

namespace plugin_nintendo.Images
{
    class RawJtexHeader
    {
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
            [2] = ImageFormats.Rgba8888(),
            [3] = ImageFormats.Rgb888(),
            [4] = ImageFormats.Rgba4444()
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(JtexFormats);

            return definition;
        }
    }
}
