using Kompression.Contract.Configuration;
using Kompression.DataClasses.Configuration;

namespace Kompression.Configuration
{
    internal class EncoderConfigurationBuilder(
        CompressionConfigurationBuilder parent,
        EncoderConfigurationOptions options)
        : IEncoderConfigurationBuilder
    {
        public ICompressionConfigurationBuilder With(CreateEncoderDelegate encoderDelegate)
        {
            options.EncoderDelegate = encoderDelegate;
            options.LempelZivEncoderDelegate = null;
            options.HuffmanEncoderDelegate = null;
            options.LempelZivHuffmanEncoderDelegate = null;

            return parent;
        }

        public ILempelZivConfigurationBuilder With(CreateLempelZivEncoderDelegate encoderDelegate)
        {
            options.EncoderDelegate = null;
            options.LempelZivEncoderDelegate = encoderDelegate;
            options.HuffmanEncoderDelegate = null;
            options.LempelZivHuffmanEncoderDelegate = null;

            return parent;
        }

        public IHuffmanConfigurationBuilder With(CreateHuffmanEncoderDelegate encoderDelegate)
        {
            options.EncoderDelegate = null;
            options.LempelZivEncoderDelegate = null;
            options.HuffmanEncoderDelegate = encoderDelegate;
            options.LempelZivHuffmanEncoderDelegate = null;

            return parent;
        }

        public ILempelZivHuffmanConfigurationBuilder With(CreateLempelZivHuffmanEncoderDelegate encoderDelegate)
        {
            options.EncoderDelegate = null;
            options.LempelZivEncoderDelegate = null;
            options.HuffmanEncoderDelegate = null;
            options.LempelZivHuffmanEncoderDelegate = encoderDelegate;

            return parent;
        }
    }
}
