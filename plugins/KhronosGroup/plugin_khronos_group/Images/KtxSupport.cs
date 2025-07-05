using BCnEncoder.Shared;
using Kanvas;
using Kanvas.Contract.Encoding;
using Konnect.Plugin.File.Image;

namespace plugin_khronos_group.Images
{
    class KtxSupport
    {
        public static readonly IDictionary<int, IColorEncoding> Formats = new Dictionary<int, IColorEncoding>
        {
            [(int)GlInternalFormat.GlCompressedRgb8Etc2] = ImageFormats.Etc2(),
            [(int)GlInternalFormat.GlCompressedRgb8PunchthroughAlpha1Etc2] = ImageFormats.Etc2A1(),
            [(int)GlInternalFormat.GlCompressedRgba8Etc2Eac] = ImageFormats.Etc2A()
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Formats);

            return definition;
        }
    }
}
