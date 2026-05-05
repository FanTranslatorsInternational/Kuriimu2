using Kanvas.Contract.Configuration;
using Kanvas.Contract.Encoding;
using Kanvas.DataClasses.Configuration;

namespace Kanvas.Configuration
{
    internal class EncodingConfigurationBuilder(
        IIndexedImageConfigurationBuilder parent,
        EncodingConfigurationOptions options)
        : IEncodingConfigurationBuilder
    {
        public IImageConfigurationBuilder With(IColorEncoding encoding)
        {
            options.ColorEncoding = encoding;
            options.IndexEncoding = null;

            return parent;
        }

        public IIndexedImageConfigurationBuilder With(IIndexEncoding encoding)
        {
            options.IndexEncoding = encoding;
            options.ColorEncoding = null;

            return parent;
        }
    }
}
