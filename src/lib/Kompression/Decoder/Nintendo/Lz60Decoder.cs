using Kompression.Contract.Decoder;
using Kompression.Exceptions;

namespace Kompression.Decoder.Nintendo
{
    public class Lz60Decoder : IDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            var buffer = new byte[4];

            _ = input.Read(buffer, 0, 4);
            if (buffer[0] != 0x60)
                throw new InvalidCompressionException("Lz60");

            int decompressedSize = buffer[1] | buffer[2] << 8 | buffer[3] << 16;

            Lz40Decoder.ReadCompressedData(input, output, decompressedSize);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
