using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.Exceptions;
using Kompression.LempelZiv.Parser;

namespace Kompression.LempelZiv
{
    public static class LZ11
    {
        public static void Decompress(Stream input, Stream output)
        {
            var compressionHeader = new byte[4];
            input.Read(compressionHeader, 0, 4);
            if (compressionHeader[0] != 0x11)
                throw new InvalidCompressionException(nameof(LZ11));

            var decompressedSize = compressionHeader[1] | (compressionHeader[2] << 8) | (compressionHeader[3] << 16);

            ReadCompressedData(input, output, decompressedSize);
        }

        public static void Compress(Stream input, Stream output)
        {
            if (input.Length > 0xFFFFFF)
                throw new InvalidOperationException("Data to compress is too long.");

            var lzFinder = new NaiveParser(3, 0x100110, 0x1000);
            var lzResults = lzFinder.Parse(ToArray(input));

            var compressionHeader = new byte[] { 0x11, (byte)(input.Length & 0xFF), (byte)((input.Length >> 8) & 0xFF), (byte)((input.Length >> 16) & 0xFF) };
            output.Write(compressionHeader, 0, 4);

            WriteCompressedData(input, output, lzResults);
        }

        private static byte[] ToArray(Stream input)
        {
            var bkPos = input.Position;
            var inputArray = new byte[input.Length];
            input.Read(inputArray, 0, inputArray.Length);
            input.Position = bkPos;

            return inputArray;
        }

        private static void ReadCompressedData(Stream input, Stream output, int decompressedSize)
        {
            int bufferLength = 0x1000, bufferOffset = 0;
            byte[] buffer = new byte[bufferLength];

            int flags = 0, mask = 1;
            while (output.Length < decompressedSize)
            {
                if (mask == 1)
                {
                    flags = input.ReadByte();
                    if (flags < 0)
                        throw new StreamTooShortException();
                    mask = 0x80;
                }
                else
                {
                    mask >>= 1;
                }

                bufferOffset = (flags & mask) > 0 ?
                    HandleCompressedBlock(input, output, buffer, bufferOffset) :
                    HandleUncompressedBlock(input, output, buffer, bufferOffset);
            }
        }

        private static int HandleUncompressedBlock(Stream input, Stream output, byte[] windowBuffer, int windowBufferOffset)
        {
            var next = input.ReadByte();
            if (next < 0)
                throw new StreamTooShortException();

            output.WriteByte((byte)next);
            windowBuffer[windowBufferOffset] = (byte)next;
            return (windowBufferOffset + 1) % windowBuffer.Length;
        }

        private static int HandleCompressedBlock(Stream input, Stream output, byte[] windowBuffer, int windowBufferOffset)
        {
            // A compressed block starts with 2 bytes; if there are there < 2 bytes left, throw error
            if (input.Length - input.Position < 2)
                throw new StreamTooShortException();

            var byte1 = (byte)input.ReadByte();
            var byte2 = (byte)input.ReadByte();

            int length, displacement;
            if (byte1 >> 4 == 0)    // 0000
            {
                (length, displacement) = HandleZeroCompressedBlock(byte1, byte2, input, output);
            }
            else if (byte1 >> 4 == 1)   // 0001
            {
                (length, displacement) = HandleOneCompressedBlock(byte1, byte2, input, output);
            }
            else    // >= 0010
            {
                (length, displacement) = HandleRemainingCompressedBlock(byte1, byte2, input, output);
            }

            var bufferIndex = windowBufferOffset + windowBuffer.Length - displacement;
            for (var i = 0; i < length; i++)
            {
                var next = windowBuffer[bufferIndex++ % windowBuffer.Length];
                output.WriteByte(next);
                windowBuffer[windowBufferOffset] = next;
                windowBufferOffset = (windowBufferOffset + 1) % windowBuffer.Length;
            }

            return windowBufferOffset;
        }

        private static (int length, int displacement) HandleZeroCompressedBlock(byte byte1, byte byte2, Stream input, Stream output)
        {
            if (input.Length - input.Position < 1)
                throw new StreamTooShortException();

            var byte3 = input.ReadByte();
            var length = (((byte1 & 0xF) << 4) | (byte2 >> 4)) + 0x11;  // max 0xFF + 0x11 = 0x110
            var displacement = (((byte2 & 0xF) << 8) | byte3) + 1;  // max 0xFFF + 1 = 0x1000

            if (displacement > output.Length)
                throw new DisplacementException(displacement, output.Length, input.Position - 3);

            return (length, displacement);
        }

        private static (int length, int displacement) HandleOneCompressedBlock(byte byte1, byte byte2, Stream input, Stream output)
        {
            if (input.Length - input.Position < 2)
                throw new StreamTooShortException();

            var byte3 = input.ReadByte();
            var byte4 = input.ReadByte();
            var length = (((byte1 & 0xF) << 12) | (byte2 << 4) | (byte3 >> 4)) + 0x111; // max 0xFFFFF + 0x111 = 0x100110
            var displacement = (((byte3 & 0xF) << 8) | byte4) + 1;  // max 0xFFF + 1 = 0x1000

            if (displacement > output.Length)
                throw new DisplacementException(displacement, output.Length, input.Position - 4);

            return (length, displacement);
        }

        private static (int length, int displacement) HandleRemainingCompressedBlock(byte byte1, byte byte2, Stream input, Stream output)
        {
            var length = (byte1 >> 4) + 1;  // max 0xF + 1 = 0x10
            var displacement = (((byte1 & 0xF) << 8) | byte2) + 1;   // max 0xFFF + 1 = 0x1000

            if (displacement > output.Length)
                throw new DisplacementException(displacement, output.Length, input.Position - 2);

            return (length, displacement);
        }

        private static void WriteCompressedData(Stream input, Stream output, IList<LzMatch> lzResults)
        {
            int bufferedBlocks = 0, blockBufferLength = 1, lzIndex = 0;
            byte[] blockBuffer = new byte[8 * 4 + 1];

            while (input.Position < input.Length)
            {
                if (bufferedBlocks >= 8)
                {
                    WriteBlockBuffer(output, blockBuffer, blockBufferLength);

                    bufferedBlocks = 0;
                    blockBufferLength = 1;
                }

                if (lzIndex < lzResults.Count && input.Position == lzResults[lzIndex].Position)
                {
                    blockBufferLength = WriteCompressedBlockToBuffer(lzResults[lzIndex], blockBuffer, blockBufferLength, bufferedBlocks);
                    input.Position += lzResults[lzIndex++].Length;
                }
                else
                {
                    blockBuffer[blockBufferLength++] = (byte)input.ReadByte();
                }

                bufferedBlocks++;
            }

            WriteBlockBuffer(output, blockBuffer, blockBufferLength);
        }

        private static int WriteCompressedBlockToBuffer(LzMatch lzMatch, byte[] blockBuffer, int blockBufferLength, int bufferedBlocks)
        {
            // mark the next block as compressed
            blockBuffer[0] |= (byte)(1 << (7 - bufferedBlocks));

            if (lzMatch.Length > 0x110)
            {
                // case 1: 1(B CD E)(F GH) + (0x111)(0x1) = (LEN)(DISP)
                blockBuffer[blockBufferLength] = 0x10;
                blockBuffer[blockBufferLength++] |= (byte)(((lzMatch.Length - 0x111) >> 12) & 0x0F);
                blockBuffer[blockBufferLength++] = (byte)(((lzMatch.Length - 0x111) >> 4) & 0xFF);
                blockBuffer[blockBufferLength] = (byte)(((lzMatch.Length - 0x111) << 4) & 0xF0);
            }
            else if (lzMatch.Length > 0x10)
            {
                // case 0; 0(B C)(D EF) + (0x11)(0x1) = (LEN)(DISP)
                blockBuffer[blockBufferLength] = 0x00;
                blockBuffer[blockBufferLength++] |= (byte)(((lzMatch.Length - 0x11) >> 4) & 0x0F);
                blockBuffer[blockBufferLength] = (byte)(((lzMatch.Length - 0x11) << 4) & 0xF0);
            }
            else
            {
                // case > 1: (A)(B CD) + (0x1)(0x1) = (LEN)(DISP)
                blockBuffer[blockBufferLength] = (byte)(((lzMatch.Length - 1) << 4) & 0xF0);
            }

            // the last 1.5 bytes are always the disp
            blockBuffer[blockBufferLength++] |= (byte)(((lzMatch.Displacement - 1) >> 8) & 0x0F);
            blockBuffer[blockBufferLength++] = (byte)((lzMatch.Displacement - 1) & 0xFF);

            return blockBufferLength;
        }

        private static void WriteBlockBuffer(Stream output, byte[] blockBuffer, int blockBufferLength)
        {
            output.Write(blockBuffer, 0, blockBufferLength);
            Array.Clear(blockBuffer, 0, blockBufferLength);
        }
    }
}
