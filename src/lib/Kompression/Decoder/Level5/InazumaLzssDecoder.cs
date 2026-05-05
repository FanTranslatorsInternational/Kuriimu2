using System.Buffers.Binary;
using Kompression.Contract.Decoder;
using Kompression.Decoder.Headerless;
using Kompression.Exceptions;

namespace Kompression.Decoder.Level5
{
    // First found in Inazuma Eleven 3: Ogre team as an ARCV compression
    public class InazumaLzssDecoder : IDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            var buffer = new byte[4];

            _ = input.Read(buffer);
            if (buffer[0] is not 0x53 || buffer[1] is not 0x53 || buffer[2] is not 0x5A || buffer[3] is not 0x4C) // "SSZL"
                throw new InvalidCompressionException("Lzss");

            input.Position = 0xC;

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
