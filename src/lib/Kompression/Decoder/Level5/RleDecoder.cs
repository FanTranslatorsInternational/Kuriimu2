using Kompression.Contract.Decoder;
using Kompression.Decoder.Headerless;
using Kompression.Exceptions;

namespace Kompression.Decoder.Level5
{
    public class RleDecoder : IDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            var buffer = new byte[4];

            _ = input.Read(buffer);
            if ((buffer[0] & 0x7) != 0x4)
                throw new InvalidCompressionException("Level5 Rle");

            int decompressedSize = buffer[0] >> 3 | buffer[1] << 5 |
                                   buffer[2] << 13 | buffer[3] << 21;

            RleHeaderlessDecoder.Decode(input, output, decompressedSize);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
