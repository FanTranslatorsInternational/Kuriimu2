using System.IO;
using Kompression.Exceptions;
using Kompression.Implementations.Decoders.Headerless;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Decoders.Nintendo
{
    public class RleDecoder : IDecoder
    {
        private readonly RleHeaderlessDecoder _decoder;

        public RleDecoder()
        {
            _decoder = new RleHeaderlessDecoder();
        }

        public void Decode(Stream input, Stream output)
        {
            var compressionHeader = new byte[4];
            input.Read(compressionHeader, 0, 4);
            if (compressionHeader[0] != 0x30)
                throw new InvalidCompressionException("Nintendo Rle");

            var decompressedSize = compressionHeader[1] | (compressionHeader[2] << 8) | (compressionHeader[3] << 16);

            _decoder.Decode(input, output, decompressedSize);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
