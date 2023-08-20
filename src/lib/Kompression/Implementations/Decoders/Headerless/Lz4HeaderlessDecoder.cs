using System;
using System.Buffers.Binary;
using System.IO;
using Kompression.IO;
using Kontract.Kompression.Interfaces.Configuration;

namespace Kompression.Implementations.Decoders.Headerless
{
    class Lz4HeaderlessDecoder : IDecoder
    {
        private byte[] buffer = new byte[4];

        public void Decode(Stream input, Stream output)
        {
            int decompSize = 0;
            while (input.Position < input.Length && !IsLastBlock(decompSize))
            {
                var blockPosition = input.Position;
                var circularBuffer = new CircularBuffer(0xFFFF);

                // Read block sizes
                var compSize = ReadBlockSizes(input, out decompSize);

                while (input.Position < blockPosition + 8 + compSize)
                {
                    var token = input.ReadByte();
                    var literalLength = token >> 4;
                    var matchLength = token & 0xF;

                    // Expand literal length
                    token = literalLength == 0xF ? 0xFF : 0;
                    while (token == 0xFF)
                    {
                        token = input.ReadByte();
                        literalLength += token;
                    }

                    // Read literals
                    var literalBuffer = new byte[literalLength];
                    input.Read(literalBuffer, 0, literalLength);
                    output.Write(literalBuffer, 0, literalLength);
                    circularBuffer.Write(literalBuffer, 0, literalLength);

                    if (input.Position >= blockPosition + 8 + compSize)
                        continue;

                    // Read displacement
                    var byte1 = input.ReadByte();
                    var displacement = (input.ReadByte() << 8) | byte1;

                    // Expand match length
                    token = matchLength == 0xF ? 0xFF : 0;
                    while (token == 0xFF)
                    {
                        token = input.ReadByte();
                        matchLength += token;
                    }

                    // Read match copy
                    circularBuffer.Copy(output, displacement, matchLength + 4);
                }
            }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }

        private int ReadBlockSizes(Stream input, out int decompBlockSize)
        {
            input.Read(buffer, 0, 4);
            decompBlockSize = BinaryPrimitives.ReadInt32LittleEndian(buffer);

            input.Read(buffer, 0, 4);
            var compSize = BinaryPrimitives.ReadInt32LittleEndian(buffer);

            if (input.Position + compSize > input.Length)
                throw new InvalidOperationException($"Invalid LZ4 block at position 0x{input.Position:X8}.");

            return compSize;
        }

        private bool IsLastBlock(int decompSize)
        {
            return decompSize < 0;
        }
    }
}
