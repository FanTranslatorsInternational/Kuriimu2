using System.IO;
using Kompression.Configuration;
using Kompression.Exceptions;
using Kompression.Implementations;
using Kompression.PatternMatch;

namespace Kompression.Implementations.Decoders
{
    public class Lz60Decoder : IDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            var compressionHeader = new byte[4];
            input.Read(compressionHeader, 0, 4);
            if (compressionHeader[0] != 0x60)
                throw new InvalidCompressionException("Lz60");

            var decompressedSize = compressionHeader[1] | (compressionHeader[2] << 8) | (compressionHeader[3] << 16);

            var lz40Decoder = new Lz40Decoder();
            lz40Decoder.ReadCompressedData(input, output, decompressedSize);
            lz40Decoder.Dispose();
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
