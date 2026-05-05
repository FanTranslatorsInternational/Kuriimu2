using Kanvas.Contract.Configuration;
using Kanvas.Contract.Encoding;
using Kanvas.DataClasses.Configuration;

namespace Kanvas.Configuration
{
    internal class PaletteEncodingConfigurationBuilder(
        IIndexedImageConfigurationBuilder parent,
        EncodingConfigurationOptions options)
        : IPaletteEncodingConfigurationBuilder
    {
        public IIndexedImageConfigurationBuilder With(IColorEncoding encoding)
        {
            options.PaletteEncoding = encoding;
            return parent;
        }
    }
}
