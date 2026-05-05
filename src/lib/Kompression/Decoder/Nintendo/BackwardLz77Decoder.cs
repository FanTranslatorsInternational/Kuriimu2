using Komponent.Contract.Enums;
using Komponent.IO;
using Komponent.Streams;
using Kompression.Contract.Decoder;
using Kompression.IO;

namespace Kompression.Decoder.Nintendo
{
    public class BackwardLz77Decoder(ByteOrder byteOrder) : IDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            input.Position = input.Length - 8;

            using var br = new BinaryReaderX(input, true, byteOrder);

            int bufferTopAndBottom = br.ReadInt32();
            int decompressedOffset = br.ReadInt32();

            int footerLength = bufferTopAndBottom >> 24;
            int compressedSize = bufferTopAndBottom & 0xFFFFFF;

            using var inputReverseStream = new ReverseStream(input, input.Length - footerLength);
            using var outputReverseStream = new ReverseStream(output, input.Length + decompressedOffset);

            int endPosition = compressedSize - footerLength;
            ReadCompressedData(inputReverseStream, outputReverseStream, endPosition);
        }

        private static void ReadCompressedData(Stream input, Stream output, long endPosition)
        {
            var circularBuffer = new CircularBuffer(0x1002);

            var codeBlock = input.ReadByte();
            var codeBlockPosition = 8;
            while (input.Position < endPosition)
            {
                if (codeBlockPosition == 0)
                {
                    codeBlock = input.ReadByte();
                    codeBlockPosition = 8;
                }

                var flag = codeBlock >> --codeBlockPosition & 0x1;
                if (flag == 0)
                    HandleUncompressedBlock(input, output, circularBuffer);
                else
                    HandleCompressedBlock(input, output, circularBuffer);
            }

            while (input.Position < input.Length)
                output.WriteByte((byte)input.ReadByte());
        }

        private static void HandleUncompressedBlock(Stream input, Stream output, CircularBuffer circularBuffer)
        {
            var next = input.ReadByte();

            output.WriteByte((byte)next);
            circularBuffer.WriteByte((byte)next);
        }

        private static void HandleCompressedBlock(Stream input, Stream output, CircularBuffer circularBuffer)
        {
            var byte1 = input.ReadByte();
            var byte2 = input.ReadByte();

            var length = (byte1 >> 4) + 3;
            var displacement = ((byte1 & 0xF) << 8 | byte2) + 3;

            circularBuffer.Copy(output, displacement, length);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
