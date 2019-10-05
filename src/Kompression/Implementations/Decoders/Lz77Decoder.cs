using System.IO;
using Kompression.Configuration;
using Kompression.IO;

namespace Kompression.Implementations.Decoders
{
    public class Lz77Decoder : IDecoder
    {
        private CircularBuffer _circularBuffer;

        public void Decode(Stream input, Stream output)
        {
            _circularBuffer = new CircularBuffer(0xFF);

            var bitReader = new BitReader(input, BitOrder.LsbFirst, 1, ByteOrder.BigEndian);
            while (bitReader.Length - bitReader.Position >= 9)
            {
                if (bitReader.ReadBit() == 0)
                    HandleUncompressedBlock(bitReader, output);
                else
                    HandleCompressedBlock(bitReader, output);
            }
        }

        private void HandleUncompressedBlock(BitReader br, Stream output)
        {
            var nextByte = (byte)br.ReadByte();

            output.WriteByte(nextByte);
            _circularBuffer.WriteByte(nextByte);
        }

        private void HandleCompressedBlock(BitReader br, Stream output)
        {
            var displacement = br.ReadByte();
            var length = br.ReadByte();
            var nextByte = (byte)br.ReadByte();

            _circularBuffer.Copy(output, displacement, length);

            // If an occurrence goes until the end of the file, 'next' still exists
            // In this case, 'next' shouldn't be written to the file
            // but there is no indicator if this symbol has to be written or not
            // According to Kuriimu, I once implemented a 0x24 static symbol for some reason
            // Maybe 'next' is 0x24 if an occurrence goes directly until the end of a file?
            // TODO: Fix overflowing symbol
            // HINT: Look at Kuriimu issue 517 and see if the used compression is this one
            output.WriteByte(nextByte);
            _circularBuffer.WriteByte(nextByte);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
