using System.Buffers.Binary;
using Kompression.Contract.Decoder;
using Kompression.Decoder.Headerless;
using Kompression.Exceptions;

namespace Kompression.Decoder
{
    public class IecpDecoder : IDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            var buffer = new byte[4];
            _ = input.Read(buffer);
            if (buffer[0] is not 0x49 || buffer[1] is not 0x45 || buffer[2] is not 0x43 || buffer[3] is not 0x50)
                throw new InvalidCompressionException("IECP");

            _ = input.Read(buffer);
            int decompressedSize = BinaryPrimitives.ReadInt32LittleEndian(buffer);

            Lzss01HeaderlessDecoder.Decode(input, output, decompressedSize);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
