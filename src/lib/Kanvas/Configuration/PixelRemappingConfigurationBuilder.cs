using Kanvas.Contract.Configuration;
using Kanvas.DataClasses.Configuration;

namespace Kanvas.Configuration
{
    internal class PixelRemappingConfigurationBuilder(
        IImageConfigurationBuilder parent,
        PixelRemappingConfigurationOptions options)
        : IRemapPixelsConfigurationBuilder
    {
        public IImageConfigurationBuilder With(CreatePixelRemapperDelegate remapDelegate)
        {
            options.PixelRemappingDelegate = remapDelegate;
            return parent;
        }
    }
}
