using System.Buffers.Binary;
using Kompression.Contract.Decoder;
using Kompression.Decoder.Headerless;

namespace Kompression.Decoder
{
    public class TalesOf01Decoder : IDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            if (input.ReadByte() != 0x01)
                throw new InvalidOperationException("This is not a tales of compression with version 1.");

            var buffer = new byte[8];

            _ = input.Read(buffer);
            _ = BinaryPrimitives.ReadInt32LittleEndian(buffer);
            int decompressedSize = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(4));

            Lzss01HeaderlessDecoder.Decode(input, output, decompressedSize);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
