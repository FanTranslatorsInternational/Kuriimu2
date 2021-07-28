using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kompression.Extensions;
using Kompression.Implementations.PriceCalculators;
using Kontract.Kompression.Configuration;
using Kontract.Kompression.Model.PatternMatch;
using Kontract.Models.IO;

namespace Kompression.Implementations.Encoders.Nintendo
{
    // TODO: Refactor block class
    public class BackwardLz77Encoder : ILzEncoder
    {
        class Block
        {
            public byte codeBlock;
            public int codeBlockPosition = 8;

            // We write all data backwards into the buffer; starting from last element down to first
            // We have 8 blocks; A block can be at max 2 bytes, defining a match
            public byte[] buffer = new byte[8 * 2];
            public int bufferLength;
        }

        private readonly ByteOrder _byteOrder;

        public BackwardLz77Encoder(ByteOrder byteOrder)
        {
            _byteOrder = byteOrder;
        }

        public void Configure(IInternalMatchOptions matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new BackwardLz77PriceCalculator())
                .FindMatches().WithinLimitations(3, 0x12, 3, 0x1002)
                .AdjustInput(input => input.Reverse());
        }

        public void Encode(Stream input, Stream output, IEnumerable<Match> matches)
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
            input.Read(origSafeBuffer);
            output.Write(origSafeBuffer);

            // Write compressed buffer
            output.Write(compressedBuffer[compressSafe..(compressSafe + newCompressedSize)]);

            // Write footer
            var bufferTopAndBottomInt = top | (bottom << 24);
            var originalBottomInt = (int)input.Length - compressedLength;

            var bufferTopAndBottom = _byteOrder == ByteOrder.LittleEndian
                ? bufferTopAndBottomInt.GetArrayLittleEndian()
                : bufferTopAndBottomInt.GetArrayBigEndian();
            var originalBottom = _byteOrder == ByteOrder.LittleEndian
                ? originalBottomInt.GetArrayLittleEndian()
                : originalBottomInt.GetArrayBigEndian();
            for (var i = 0; i < compFooterOffset - padOffset; i++)
                output.WriteByte(0xFF);
            output.Write(bufferTopAndBottom, 0, 4);
            output.Write(originalBottom, 0, 4);
        }

        private int CalculateCompressedLength(long uncompressedLength, Match[] matches)
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

        private byte[] Compress(Stream input, IList<Match> matches, int compressedLength)
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
                    if (block.codeBlockPosition == 0)
                        bufferPosition -= WriteAndResetBuffer(buffer, bufferPosition, block);

                    block.codeBlockPosition--;
                    input.Position = --inputPosition;
                    block.buffer[block.bufferLength++] = (byte)input.ReadByte();
                }

                // Write match
                var byte1 = ((byte)(match.Length - 3) << 4) | (byte)((match.Displacement - 3) >> 8);
                var byte2 = match.Displacement - 3;

                if (block.codeBlockPosition == 0)
                    bufferPosition -= WriteAndResetBuffer(buffer, bufferPosition, block);

                block.codeBlock |= (byte)(1 << --block.codeBlockPosition);
                block.buffer[block.bufferLength++] = (byte)byte1;
                block.buffer[block.bufferLength++] = (byte)byte2;

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

        private int CalculateSafeCompressedSize(byte[] compressedBuffer, int decompressedSize, out int origSafe)
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

        private int WriteAndResetBuffer(byte[] buffer, int bufferPosition, Block block)
        {
            var blockLength = block.bufferLength + 1;

            // Write data to output
            buffer[--bufferPosition] = block.codeBlock;
            for (var i = 0; i < block.bufferLength; i++)
                buffer[--bufferPosition] = block.buffer[i];

            // Reset codeBlock and buffer
            block.codeBlock = 0;
            block.codeBlockPosition = 8;
            Array.Clear(block.buffer, 0, block.bufferLength);
            block.bufferLength = 0;

            return blockLength;
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
