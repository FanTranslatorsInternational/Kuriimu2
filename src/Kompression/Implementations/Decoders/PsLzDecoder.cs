using System;
using System.IO;
using Kompression.IO;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Decoders
{
    /* Found in SMT Nocturne on the PS2 */
    class PsLzDecoder : IDecoder
    {
        private CircularBuffer _circularBuffer;

        public void Decode(Stream input, Stream output)
        {
            _circularBuffer = new CircularBuffer(0xFFFF);

            int mode;
            while ((mode = input.ReadByte()) != 0xFF)
            {
                var length = mode & 0x1F;
                if (length == 0)
                    length = ReadInt16Le(input);

                var buffer = new byte[length];

                switch (mode >> 5)
                {
                    // Raw bytes
                    case 0:
                        input.Read(buffer, 0, length);

                        output.Write(buffer, 0, length);
                        _circularBuffer.Write(buffer, 0, length);
                        break;

                    // 0-only RLE
                    case 1:
                        output.Write(buffer, 0, length);
                        _circularBuffer.Write(buffer, 0, length);
                        break;

                    // 1-byte displacement LZ
                    case 3:
                        var offset = input.ReadByte();

                        _circularBuffer.Copy(output, offset, length);
                        break;

                    // 2-byte displacement LZ
                    case 4:
                        var offset1 = ReadInt16Le(input);

                        _circularBuffer.Copy(output, offset1, length);
                        break;

                    // 2-byte RLE 0-discarding
                    case 5:
                        buffer = new byte[length * 2];

                        for (var i = 0; i < length; i++)
                            buffer[i * 2] = (byte)input.ReadByte();

                        output.Write(buffer, 0, buffer.Length);
                        _circularBuffer.Write(buffer, 0, buffer.Length);
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown mode {mode >> 5} in PsLz at position 0x{input.Position:X8}.");
                }
            }
        }

        private int ReadInt16Le(Stream input)
        {
            return input.ReadByte() | (input.ReadByte() << 8);
        }

        public void Dispose()
        {
            _circularBuffer?.Dispose();
        }
    }
}
