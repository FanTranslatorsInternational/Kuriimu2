using Kanvas;
using Kanvas.Contract.Encoding;
using Konnect.Plugin.File.Image;

namespace plugin_nintendo.Images
{
    struct RawJtexHeader
    {
        public int format;
        public int width;
        public int height;
        public int paddedWidth;
        public int paddedHeight;
    }

    public class RawJtexSupport
    {
        private static readonly Dictionary<int, IColorEncoding> JtexFormats = new()
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
