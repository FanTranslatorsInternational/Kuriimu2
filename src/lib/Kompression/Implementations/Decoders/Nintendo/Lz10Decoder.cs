using System.IO;
using Kompression.Exceptions;
using Kompression.Implementations.Decoders.Headerless;
using Kontract.Kompression.Interfaces.Configuration;

namespace Kompression.Implementations.Decoders.Nintendo
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
            if (compressionHeader[0] != 0x10)
                throw new InvalidCompressionException("Nintendo Lz10");

            var decompressedSize = compressionHeader[1] | (compressionHeader[2] << 8) | (compressionHeader[3] << 16);

            _decoder.Decode(input,output,decompressedSize);
        }

        public void Dispose()
        {
        }
    }
}
