using System.IO;
using Kompression.Exceptions;

namespace Kompression.LempelZiv.Decoders
{
    class Lz60Decoder:ILzDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            var compressionHeader = new byte[4];
            input.Read(compressionHeader, 0, 4);
            if (compressionHeader[0] != 0x60)
                throw new InvalidCompressionException(nameof(LZ60));

            var decompressedSize = compressionHeader[1] | (compressionHeader[2] << 8) | (compressionHeader[3] << 16);

            new Lz40Decoder().ReadCompressedData(input,output,decompressedSize);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
