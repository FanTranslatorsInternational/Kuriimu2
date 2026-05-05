using Komponent.Contract.Enums;
using Komponent.IO;
using Kompression.Contract.Decoder;
using Kompression.IO;

namespace Kompression.Decoder
{
    public class Lz77Decoder : IDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            var circularBuffer = new CircularBuffer(0xFF);

            var bitReader = new BinaryBitReader(input, BitOrder.LeastSignificantBitFirst, 1, ByteOrder.BigEndian);
            while (bitReader.Length - bitReader.Position >= 9)
            {
                if (bitReader.ReadBit() == 0)
                    HandleUncompressedBlock(bitReader, output, circularBuffer);
                else
                    HandleCompressedBlock(bitReader, output, circularBuffer);
            }
        }

        private static void HandleUncompressedBlock(BinaryBitReader br, Stream output, CircularBuffer circularBuffer)
        {
            var nextByte = (byte)br.ReadByte();

            output.WriteByte(nextByte);
            circularBuffer.WriteByte(nextByte);
        }

        private static void HandleCompressedBlock(BinaryBitReader br, Stream output, CircularBuffer circularBuffer)
        {
            var displacement = br.ReadByte();
            var length = br.ReadByte();
            var nextByte = (byte)br.ReadByte();

            circularBuffer.Copy(output, displacement, length);

            // If an occurrence goes until the end of the file, 'next' still exists
            // In this case, 'next' shouldn't be written to the file
            // but there is no indicator if this symbol has to be written or not
            // According to Kuriimu, I once implemented a 0x24 static symbol for some reason
            // Maybe 'next' is 0x24 if an occurrence goes directly until the end of a file?
            // TODO: Fix overflowing symbol
            // HINT: Look at Kuriimu issue 517 and see if the used compression is this one
            output.WriteByte(nextByte);
            circularBuffer.WriteByte(nextByte);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
