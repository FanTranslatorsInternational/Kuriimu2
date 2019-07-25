using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.Exceptions;
using Kompression.LempelZiv.Parser;

/* The same as LZ40 just with another magic num */

namespace Kompression.LempelZiv
{
    public static class LZ40
    {
        public static void Decompress(Stream input, Stream output)
        {
            var compressionHeader = new byte[4];
            input.Read(compressionHeader, 0, 4);
            if (compressionHeader[0] != 0x40)
                throw new InvalidCompressionException(nameof(LZ40));

            var decompressedSize = compressionHeader[1] | (compressionHeader[2] << 8) | (compressionHeader[3] << 16);

            ReadCompressedData(input, output, decompressedSize);
        }

        public static void Compress(Stream input, Stream output)
        {
            if (input.Length > 0xFFFFFF)
                throw new InvalidOperationException("Data to compress is too long.");

            var lzFinder = new NaiveParser(3, 0x10010F, 0xFFF);
            var lzResults = lzFinder.Parse(ToArray(input));

            var compressionHeader = new byte[] { 0x40, (byte)(input.Length & 0xFF), (byte)((input.Length >> 8) & 0xFF), (byte)((input.Length >> 16) & 0xFF) };
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

        internal static void ReadCompressedData(Stream input, Stream output, int decompressedSize)
        {
            int bufferLength = 0xFFF, bufferOffset = 0;
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

            int displacement = (byte2 << 4) | (byte1 >> 4);    // max 0xFFF
            if (displacement > output.Length)
                throw new DisplacementException(displacement, output.Length, input.Position - 2);

            int length;
            if ((byte1 & 0xF) == 0)    // 0000
            {
                length = HandleZeroCompressedBlock(input, output);
            }
            else if ((byte1 & 0xF) == 1)   // 0001
            {
                length = HandleOneCompressedBlock(input, output);
            }
            else    // >= 0010
            {
                length = byte1 & 0xF;
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

        private static int HandleZeroCompressedBlock(Stream input, Stream output)
        {
            if (input.Length - input.Position < 1)
                throw new StreamTooShortException();

            var byte3 = input.ReadByte();
            var length = byte3 + 0x10;  // max 0xFF + 0x10 = 0x10F

            return length;
        }

        private static int HandleOneCompressedBlock(Stream input, Stream output)
        {
            if (input.Length - input.Position < 2)
                throw new StreamTooShortException();

            var byte3 = input.ReadByte();
            var byte4 = input.ReadByte();
            var length = ((byte4 << 8) | byte3) + 0x110; // max 0xFFFF + 0x110 = 0x1010F

            return length;
        }

        internal static void WriteCompressedData(Stream input, Stream output, IList<LzMatch> lzResults)
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

            // the last 1.5 bytes are always the displacement
            blockBuffer[blockBufferLength] = (byte)((lzMatch.Displacement & 0x0F) << 4);
            blockBuffer[blockBufferLength + 1] = (byte)((lzMatch.Displacement >> 4) & 0xFF);

            if (lzMatch.Length > 0x10F)
            {
                // case 1: (A)1 (CD) (EF GH) + (0x0)(0x110) = (DISP = A-C-D)(LEN = E-F-G-H)
                blockBuffer[blockBufferLength] |= 0x01;
                blockBufferLength += 2;
                blockBuffer[blockBufferLength++] = (byte)((lzMatch.Length - 0x110) & 0xFF);
                blockBuffer[blockBufferLength] = (byte)(((lzMatch.Length - 0x110) >> 8) & 0xFF);
            }
            else if (lzMatch.Length > 0xF)
            {
                // case 0; (A)0 (CD) (EF) + (0x0)(0x10) = (DISP = A-C-D)(LEN = E-F)
                blockBuffer[blockBufferLength] |= 0x00;
                blockBufferLength += 2;
                blockBuffer[blockBufferLength] = (byte)((lzMatch.Length - 0x10) & 0xFF);
            }
            else
            {
                // case > 1: (A)(B) (CD) + (0x0)(0x0) = (DISP = A-C-D)(LEN = B)
                blockBuffer[blockBufferLength++] |= (byte)(lzMatch.Length & 0x0F);
            }

            blockBufferLength++;
            return blockBufferLength;
        }

        private static void WriteBlockBuffer(Stream output, byte[] blockBuffer, int blockBufferLength)
        {
            output.Write(blockBuffer, 0, blockBufferLength);
            Array.Clear(blockBuffer, 0, blockBufferLength);
        }
    }
}
