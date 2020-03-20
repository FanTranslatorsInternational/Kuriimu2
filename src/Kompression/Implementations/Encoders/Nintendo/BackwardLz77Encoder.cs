using System;
using System.IO;
using System.Linq;
using Komponent.IO.Streams;
using Kompression.Extensions;
using Kontract.Kompression;
using Kontract.Kompression.Configuration;
using Kontract.Kompression.Model.PatternMatch;
using Kontract.Models.IO;

namespace Kompression.Implementations.Encoders.Nintendo
{
    // TODO: Refactor block class
    public class BackwardLz77Encoder : IEncoder
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

        private readonly IMatchParser _matchParser;
        private readonly ByteOrder _byteOrder;

        public BackwardLz77Encoder(IMatchParser matchParser, ByteOrder byteOrder)
        {
            _byteOrder = byteOrder;
            _matchParser = matchParser;
        }

        public void Encode(Stream input, Stream output)
        {
            var matches = _matchParser.ParseMatches(input).ToArray();

            var compressedLength = PreCalculateCompressedLength(input.Length, matches);

            var block = new Block();

            using (var inputReverseStream = new ReverseStream(input, input.Length))
            using (var reverseOutputStream = new ReverseStream(output, compressedLength))
            {
                foreach (var match in matches)
                {
                    while (match.Position > inputReverseStream.Position)
                    {
                        if (block.codeBlockPosition == 0)
                            WriteAndResetBuffer(reverseOutputStream, block);

                        block.codeBlockPosition--;
                        block.buffer[block.bufferLength++] = (byte)inputReverseStream.ReadByte();
                    }

                    var byte1 = ((byte)(match.Length - 3) << 4) | (byte)((match.Displacement - 3) >> 8);
                    var byte2 = match.Displacement - 3;

                    if (block.codeBlockPosition == 0)
                        WriteAndResetBuffer(reverseOutputStream, block);

                    block.codeBlock |= (byte)(1 << --block.codeBlockPosition);
                    block.buffer[block.bufferLength++] = (byte)byte1;
                    block.buffer[block.bufferLength++] = (byte)byte2;

                    inputReverseStream.Position += match.Length;
                }

                // Write any data after last match, to the buffer
                while (inputReverseStream.Position < inputReverseStream.Length)
                {
                    if (block.codeBlockPosition == 0)
                        WriteAndResetBuffer(reverseOutputStream, block);

                    block.codeBlockPosition--;
                    block.buffer[block.bufferLength++] = (byte)inputReverseStream.ReadByte();
                }

                // Flush remaining buffer to stream
                WriteAndResetBuffer(reverseOutputStream, block);

                output.Position = compressedLength;
                WriteFooterInformation(input, output);
            }
        }

        private int PreCalculateCompressedLength(long uncompressedLength, Match[] matches)
        {
            var length = 0;
            var writtenCodes = 0;
            foreach (var match in matches)
            {
                var rawLength = uncompressedLength - match.Position - 1;

                // Raw data before match
                length += (int)rawLength;
                writtenCodes += (int)rawLength;
                uncompressedLength -= rawLength;

                // Match data
                writtenCodes++;
                length += 2;
                uncompressedLength -= match.Length;
            }

            length += (int)uncompressedLength;
            writtenCodes += (int)uncompressedLength;

            length += writtenCodes / 8;
            length += writtenCodes % 8 > 0 ? 1 : 0;

            return length;
        }

        private void WriteFooterInformation(Stream input, Stream output)
        {
            // Remember count of padding bytes
            var padding = 0;
            if (output.Length % 4 != 0)
                padding = (int)(4 - output.Position % 4);

            // Write padding
            for (var i = 0; i < padding; i++)
                output.WriteByte(0xFF);

            // Write footer
            var compressedSize = output.Position + 8;
            var bufferTopAndBottomInt = ((8 + padding) << 24) | (int)(compressedSize & 0xFFFFFF);
            var originalBottomInt = (int)(input.Length - compressedSize);

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
