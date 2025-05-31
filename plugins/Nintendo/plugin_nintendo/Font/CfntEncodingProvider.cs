using Kanvas.Contract.Encoding;
using Kanvas;
using Komponent.Contract.Enums;
using Konnect.Plugin.File.Image;

namespace plugin_nintendo.Font
{
    class CfntEncodingProvider
    {
        private readonly Dictionary<int, IColorEncoding> _ctrFormat = new()
        {
            [0] = ImageFormats.Rgba8888(),
            [1] = ImageFormats.Rgb888(),
            [2] = ImageFormats.Rgba5551(),
            [3] = ImageFormats.Rgb565(),
            [4] = ImageFormats.Rgba4444(),
            [5] = ImageFormats.La88(),
            [6] = ImageFormats.Rg88(),
            [7] = ImageFormats.L8(),
            [8] = ImageFormats.A8(),
            [9] = ImageFormats.La44(),
            [10] = ImageFormats.L4(BitOrder.LeastSignificantBitFirst),
            [11] = ImageFormats.A4(BitOrder.LeastSignificantBitFirst),
            [12] = ImageFormats.Etc1(true),
            [13] = ImageFormats.Etc1A4(true)
        };

        public EncodingDefinition GetEncodingDefinitions()
        {
            var encodingDefinition = new EncodingDefinition();
            encodingDefinition.AddColorEncodings(_ctrFormat);

            return encodingDefinition;
        }
    }
}
