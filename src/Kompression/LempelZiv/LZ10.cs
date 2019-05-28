using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kompression.Exceptions;

namespace Kompression.LempelZiv
{
    /// <summary>
    /// Provides methods for handling LZ10 compression.
    /// </summary>
    public static class LZ10
    {
        /// <summary>
        /// Decompresses LZ10 compressed data.
        /// </summary>
        /// <param name="input">The stream to decompress.</param>
        /// <param name="output">The stream to write the decompressed data into.</param>
        /// <exception cref="T:Kompression.Exceptions.StreamTooShortException">If stream is too short for decompression.</exception>
        public static void Decompress(Stream input, Stream output)
        {
            var compressionHeader = new byte[4];
            input.Read(compressionHeader, 0, 4);
            if (compressionHeader[0] != 0x10)
                throw new InvalidCompressionException(nameof(LZ10));

            var decompressedSize = compressionHeader[1] | (compressionHeader[2] << 8) | (compressionHeader[3] << 16);

            ReadCompressedData(input, output, decompressedSize);
        }

        /// <summary>
        /// Compresses data with LZ10.
        /// </summary>
        /// <param name="input">The stream to compress.</param>
        /// <param name="output">The stream to compress into.</param>
        public static void Compress(Stream input, Stream output)
        {
            if (input.Length > 0xFFFFFF)
                throw new InvalidOperationException("Data to compress is too long.");

            var compressionHeader = new byte[] { 0x10, (byte)(input.Length & 0xFF), (byte)((input.Length >> 8) & 0xFF), (byte)((input.Length >> 16) & 0xFF) };
            output.Write(compressionHeader, 0, 4);

            WriteCompressedData(input, output);
        }

        internal static void ReadCompressedData(Stream input, Stream output, int decompressedSize)
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

            var byte1 = input.ReadByte();
            var byte2 = input.ReadByte();

            // The number of bytes to copy
            var length = (byte1 >> 4) + 3;

            // From where the bytes should be copied (relatively)
            var displacement = (((byte1 & 0x0F) << 8) | byte2) + 1;

            if (displacement > output.Length)
                throw new DisplacementException(displacement, output.Length, input.Position - 2);

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

        internal static void WriteCompressedData(Stream input, Stream output)
        {
            var lzResults = GetLzResults(input);

            int bufferedBlocks = 0, blockBufferLength = 1, lzIndex = 0;
            byte[] blockBuffer = new byte[8 * 2 + 1];

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

        private static IList<LzResult> GetLzResults(Stream input)
        {
            var inputBuffer = new byte[input.Length - input.Position];
            var inputPosBk = input.Position;
            input.Read(inputBuffer, 0, inputBuffer.Length);
            input.Position = inputPosBk;
            return Common.FindOccurrences(inputBuffer, 0x1000, 3, 0x12).
                OrderBy(x => x.Position).
                ToList();
        }

        private static int WriteCompressedBlockToBuffer(LzResult lzResult, byte[] blockBuffer, int blockBufferLength, int bufferedBlocks)
        {
            blockBuffer[0] |= (byte)(1 << (7 - bufferedBlocks));

            blockBuffer[blockBufferLength] = (byte)(((lzResult.Length - 3) << 4) & 0xF0);
            blockBuffer[blockBufferLength++] |= (byte)(((lzResult.Displacement - 1) >> 8) & 0x0F);
            blockBuffer[blockBufferLength++] = (byte)((lzResult.Displacement - 1) & 0xFF);

            return blockBufferLength;
        }

        private static void WriteBlockBuffer(Stream output, byte[] blockBuffer, int blockBufferLength)
        {
            output.Write(blockBuffer, 0, blockBufferLength);
            Array.Clear(blockBuffer, 0, blockBufferLength);
        }
    }
}
