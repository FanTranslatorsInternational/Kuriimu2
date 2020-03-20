using System.IO;
using Kompression.Exceptions;
using Kompression.Implementations.Decoders.Headerless;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Decoders.Level5
{
    public class Lz10Decoder : IDecoder
    {
        private readonly Lz10HeaderlessDecoder _decoder;

        public Lz10Decoder()
        {
            _decoder = new Lz10HeaderlessDecoder();
        }

        public void Decode(Stream input, Stream output)
        {
            var compressionHeader = new byte[4];
            input.Read(compressionHeader, 0, 4);
            if ((compressionHeader[0] & 0x7) != 0x1)
                throw new InvalidCompressionException("Level5 Lz10");

            var decompressedSize = (compressionHeader[0] >> 3) | (compressionHeader[1] << 5) |
                                   (compressionHeader[2] << 13) | (compressionHeader[3] << 21);

            _decoder.Decode(input, output, decompressedSize);
        }

        public void Dispose()
        {
        }
    }
}
