using Komponent.Contract.Enums;
using Komponent.IO;
using Kompression.Contract.Configuration;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder;
using Kompression.Encoder.LempelZiv.PriceCalculators;

namespace Kompression.Encoder.Nintendo
{
    public class BackwardLz77Encoder(ByteOrder byteOrder) : ILempelZivEncoder
    {
        internal class Block
        {
            public byte CodeBlock;
            public int CodeBlockPosition = 8;

            // We write all data backwards into the buffer; starting from last element down to first
            // We have 8 blocks; A block can be at max 2 bytes, defining a match
            public readonly byte[] Buffer = new byte[8 * 2];
            public int BufferLength;
        }

        public void Configure(ILempelZivEncoderOptionsBuilder matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new BackwardLz77PriceCalculator())
                .FindPatternMatches().WithinLimitations(3, 0x12, 3, 0x1002)
                .AdjustInput(input => input.Reverse());
        }

        public void Encode(Stream input, Stream output, IEnumerable<LempelZivMatch> matches)
        {
            var matchArray = matches.ToArray();

            // Compress file into memory buffer
            var compressedLength = CalculateCompressedLength(input.Length, matchArray);
            var compressedBuffer = Compress(input, matchArray, compressedLength);

            // Determine safe compressed size until destination catches up
            var compressSafe = CalculateSafeCompressedSize(compressedBuffer, (int)input.Length, out var origSafe);

            // Write compressed data
            var newCompressedSize = compressedLength - compressSafe;
            var padOffset = origSafe + newCompressedSize;
            var compFooterOffset = (padOffset + 3) / 4 * 4;
            compressedLength = compFooterOffset + 8;
            var top = compressedLength - origSafe;
            var bottom = compressedLength - padOffset;

            // Write uncompressed start
            var origSafeBuffer = new byte[origSafe];
            input.Position = 0;
            _ = input.Read(origSafeBuffer);
            output.Write(origSafeBuffer);

            // Write compressed buffer
            output.Write(compressedBuffer.AsSpan(compressSafe, newCompressedSize));

            // Write footer
            var bufferTopAndBottomInt = top | bottom << 24;
            var originalBottomInt = (int)input.Length - compressedLength;

            using var bw = new BinaryWriterX(output, true, byteOrder);

            for (var i = 0; i < compFooterOffset - padOffset; i++)
                output.WriteByte(0xFF);

            bw.Write(bufferTopAndBottomInt);
            bw.Write(originalBottomInt);
        }

        private static int CalculateCompressedLength(long uncompressedLength, LempelZivMatch[] matches)
        {
            var result = 0;

            var lastMatchPosition = uncompressedLength;

            foreach (var match in matches)
            {
                // Add raw bytes
                if (lastMatchPosition > match.Position)
                {
                    var rawLength = (int)(lastMatchPosition - match.Position);
                    result += rawLength * 9;
                }

                result += 17;
                lastMatchPosition = match.Position - match.Length;
            }

            return result / 8 + (result % 8 > 0 ? 1 : 0) + (int)lastMatchPosition;
        }

        private static byte[] Compress(Stream input, IList<LempelZivMatch> matches, int compressedLength)
        {
            var buffer = new byte[compressedLength];
            var bufferPosition = compressedLength;
            var inputPosition = input.Length;

            var block = new Block();
            foreach (var match in matches)
            {
                while (inputPosition > match.Position)
                {
                    // Write literals
                    if (block.CodeBlockPosition == 0)
                        bufferPosition -= WriteAndResetBuffer(buffer, bufferPosition, block);

                    block.CodeBlockPosition--;
                    input.Position = --inputPosition;
                    block.Buffer[block.BufferLength++] = (byte)input.ReadByte();
                }

                // Write match
                var byte1 = (byte)(match.Length - 3) << 4 | (byte)(match.Displacement - 3 >> 8);
                var byte2 = match.Displacement - 3;

                if (block.CodeBlockPosition == 0)
                    bufferPosition -= WriteAndResetBuffer(buffer, bufferPosition, block);

                block.CodeBlock |= (byte)(1 << --block.CodeBlockPosition);
                block.Buffer[block.BufferLength++] = (byte)byte1;
                block.Buffer[block.BufferLength++] = (byte)byte2;

                inputPosition -= match.Length;
            }

            // Flush remaining buffer to stream
            WriteAndResetBuffer(buffer, bufferPosition, block);

            // Write remaining literals
            while (inputPosition > 0)
            {
                input.Position = --inputPosition;
                buffer[--bufferPosition] = (byte)input.ReadByte();
            }

            return buffer;
        }

        private static int CalculateSafeCompressedSize(byte[] compressedBuffer, int decompressedSize, out int origSafe)
        {
            origSafe = 0;
            var compressSafe = 0;

            var compressedBufferPosition = compressedBuffer.Length;
            var finished = false;

            while (decompressedSize > 0)
            {
                var flag = compressedBuffer[--compressedBufferPosition];
                for (var i = 0; i < 8; i++)
                {
                    if ((flag << i & 0x80) == 0)
                    {
                        compressedBufferPosition--;
                        decompressedSize--;
                    }
                    else
                    {
                        var size = (compressedBuffer[--compressedBufferPosition] >> 4 & 0x0F) + 3;

                        compressedBufferPosition--;
                        decompressedSize -= size;

                        if (decompressedSize < compressedBufferPosition)
                        {
                            origSafe = decompressedSize;
                            compressSafe = compressedBufferPosition;
                            finished = true;

                            break;
                        }
                    }

                    if (decompressedSize <= 0)
                        break;
                }

                if (finished)
                    break;
            }

            return compressSafe;
        }

        private static int WriteAndResetBuffer(byte[] buffer, int bufferPosition, Block block)
        {
            var blockLength = block.BufferLength + 1;

            // Write data to output
            buffer[--bufferPosition] = block.CodeBlock;
            for (var i = 0; i < block.BufferLength; i++)
                buffer[--bufferPosition] = block.Buffer[i];

            // Reset codeBlock and buffer
            block.CodeBlock = 0;
            block.CodeBlockPosition = 8;
            Array.Clear(block.Buffer, 0, block.BufferLength);
            block.BufferLength = 0;

            return blockLength;
        }
    }
}
