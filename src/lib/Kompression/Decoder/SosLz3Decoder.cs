using System.Buffers.Binary;
using Kompression.Contract.Decoder;
using Kompression.IO;

namespace Kompression.Decoder
{
    internal class SosLz3Decoder : IDecoder
    {
        public void Decode(Stream input, Stream output)
        {
            var buffer = new byte[4];
            _ = input.Read(buffer);

            (int lzType, int decompSize) = (buffer[3] >> 6, BinaryPrimitives.ReadInt32LittleEndian(buffer) & 0x3FFFFFFF);
            if (lzType != 3)
                throw new InvalidOperationException("File is not compressed with SosLz3.");

            Decode(input, output, decompSize);
        }

        private static void Decode(Stream input, Stream output, int decompSize)
        {
            var buffer = new CircularBuffer(0xFFFF);

            while (output.Position < decompSize)
            {
                var flag = (byte)input.ReadByte();
                var (lLen, cLen) = (flag >> 4, flag & 15);

                // Read literals
                if (lLen == 15)
                    ReadVar(input, ref lLen);

                if (lLen > 0)
                {
                    var literalBuffer = new byte[lLen];
                    _ = input.Read(literalBuffer);

                    buffer.Write(literalBuffer, 0, literalBuffer.Length);
                    output.Write(literalBuffer);
                }

                if (output.Position >= decompSize)
                    break;

                // Read compressed block
                var (b1, b2) = (input.ReadByte(), input.ReadByte());
                var offset = b2 << 8 | b1;

                if (cLen == 15)
                    ReadVar(input, ref cLen);

                buffer.Copy(output, offset, cLen + 4);
            }
        }

        private static void ReadVar(Stream fs, ref int start)
        {
            int value;
            do
            {
                value = fs.ReadByte();
                start += value;
            } while (value == 255);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
