using Kompression.Contract.Configuration;
using Kompression.DataClasses.Configuration;

namespace Kompression.Configuration
{
    internal class DecoderConfigurationBuilder(
        CompressionConfigurationBuilder parent,
        DecoderConfigurationOptions options)
        : IDecoderConfigurationBuilder
    {
        public ICompressionConfigurationBuilder With(CreateDecoderDelegate decoderDelegate)
        {
            options.DecoderDelegate = decoderDelegate;
            return parent;
        }
    }
}
