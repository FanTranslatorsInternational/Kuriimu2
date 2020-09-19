using System.IO;
using Kompression.Exceptions;
using Kompression.Implementations.Decoders.Headerless;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Decoders.Level5
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
            if ((compressionHeader[0]&0x7) != 0x4)
                throw new InvalidCompressionException("Level5 Rle");

            var decompressedSize = (compressionHeader[0] >> 3) | (compressionHeader[1] << 5) |
                                   (compressionHeader[2] << 13) | (compressionHeader[3] << 21);

            _decoder.Decode(input, output, decompressedSize);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
