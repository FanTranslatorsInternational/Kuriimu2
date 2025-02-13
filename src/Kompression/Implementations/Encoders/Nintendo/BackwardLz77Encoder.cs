using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO.Streams;
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

            var compressedLength = CalculateCompressedLength(input.Length, matchArray, out var lastRawLength);

            var block = new Block();

            using (var inputReverseStream = new ReverseStream(input, input.Length))
            using (var outputReverseStream = new ReverseStream(output, compressedLength + lastRawLength))
            {
                foreach (var match in matchArray)
                {
                    while (match.Position < input.Length - inputReverseStream.Position)
                    {
                        if (block.codeBlockPosition == 0)
                            WriteAndResetBuffer(outputReverseStream, block);

                        block.codeBlockPosition--;
                        block.buffer[block.bufferLength++] = (byte)inputReverseStream.ReadByte();
                    }

                    var byte1 = ((byte)(match.Length - 3) << 4) | (byte)((match.Displacement - 3) >> 8);
                    var byte2 = match.Displacement - 3;

                    if (block.codeBlockPosition == 0)
                        WriteAndResetBuffer(outputReverseStream, block);

                    block.codeBlock |= (byte)(1 << --block.codeBlockPosition);
                    block.buffer[block.bufferLength++] = (byte)byte1;
                    block.buffer[block.bufferLength++] = (byte)byte2;

                    inputReverseStream.Position += match.Length;
                }

                // Flush remaining buffer to stream
                WriteAndResetBuffer(outputReverseStream, block);

                // Write any data after last match as raw unbuffered data
                while (inputReverseStream.Position < inputReverseStream.Length)
                    outputReverseStream.WriteByte((byte)inputReverseStream.ReadByte());

                output.Position = compressedLength + lastRawLength;
                WriteFooterInformation(input, output, lastRawLength);
            }
        }

        private int CalculateCompressedLength(long uncompressedLength, Match[] matches, out int lastRawLength)
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

            lastRawLength = (int)lastMatchPosition;
            return result / 8 + (result % 8 > 0 ? 1 : 0);
        }

        private void WriteFooterInformation(Stream input, Stream output, int lastRawLength)
        {
            // Remember count of padding bytes
            var padding = 0;
            if (output.Length % 4 != 0)
                padding = (int)(4 - output.Position % 4);

            // Write padding
            for (var i = 0; i < padding; i++)
                output.WriteByte(0xFF);

            // Write footer
            var compressedSize = output.Length + 8 - lastRawLength;
            var bufferTopAndBottomInt = ((8 + padding) << 24) | (int)(compressedSize & 0xFFFFFF);
            var originalBottomInt = (int)(input.Length - (output.Length + 8));

            var bufferTopAndBottom = _byteOrder == ByteOrder.LittleEndian
                ? bufferTopAndBottomInt.GetArrayLittleEndian()
                : bufferTopAndBottomInt.GetArrayBigEndian();
            var originalBottom = _byteOrder == ByteOrder.LittleEndian
                ? originalBottomInt.GetArrayLittleEndian()
                : originalBottomInt.GetArrayBigEndian();
            output.Write(bufferTopAndBottom, 0, 4);
            output.Write(originalBottom, 0, 4);
        }

        private void WriteAndResetBuffer(Stream output, Block block)
        {
            // Write data to output
            output.WriteByte(block.codeBlock);
            output.Write(block.buffer, 0, block.bufferLength);

            // Reset codeBlock and buffer
            block.codeBlock = 0;
            block.codeBlockPosition = 8;
            Array.Clear(block.buffer, 0, block.bufferLength);
            block.bufferLength = 0;
        }

        public void Dispose()
        {
        }
    }
}
