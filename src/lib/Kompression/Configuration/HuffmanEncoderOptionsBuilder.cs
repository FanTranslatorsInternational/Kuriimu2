using Kompression.Contract.Configuration;
using Kompression.DataClasses.Configuration;

namespace Kompression.Configuration
{
    internal class HuffmanEncoderOptionsBuilder(HuffmanOptions options) : IHuffmanEncoderOptionsBuilder
    {
        private readonly HuffmanOptions _options = options;
        private readonly HuffmanEncoderOptions _encoderOptions = new();
    }
}
