using System;
using System.IO;
using Kompression.PatternMatch;

namespace Kompression.Implementations.Encoders
{
    class LzssVlcEncoder : IPatternMatchEncoder
    {
        public void Encode(Stream input, Stream output, Match[] matches)
        {
            var decompressedSize = CreateVlc((int)input.Length);
            var unk1 = CreateVlc(0x19);
            var unk2 = CreateVlc(0);

            output.Write(decompressedSize, 0, decompressedSize.Length);
            output.Write(unk1, 0, unk1.Length);
            output.Write(unk2, 0, unk2.Length);

            WriteCompressedData(input, output, matches);
        }

        private void WriteCompressedData(Stream input, Stream output, Match[] matches)
        {
            var lzIndex = 0;
            while (input.Position < input.Length)
            {
                if (lzIndex >= matches.Length)
                {
                    // If we have still uncompressed data, but no compressed blocks follow
                    CompressLastRawBlock(input, output);
                }
                else
                {
                    // If we have uncompressed data followed by n compressed blocks

                    // Variable _length encode compressed block count and raw data size
                    var rawSize = (int)(matches[lzIndex].Position - input.Position);

                    var compressedBlocks = 0;
                    var positionOffset = 0;
                    while (lzIndex + compressedBlocks < matches.Length &&
                           matches[lzIndex + compressedBlocks].Position == input.Position + rawSize + positionOffset)
                    {
                        positionOffset += (int)matches[lzIndex + compressedBlocks].Length;
                        compressedBlocks++;
                    }

                    WriteBlockSizes(output, rawSize, compressedBlocks);
                    WriteBlocks(input, output, rawSize, new Span<Match>(matches, lzIndex, compressedBlocks));

                    lzIndex += compressedBlocks;
                }
            }
        }

        private void CompressLastRawBlock(Stream input, Stream output)
        {
            var rawSize = (int)(input.Length - input.Position);
            if (rawSize > 0 && rawSize < 0x10)
            {
                output.Write(new[] { (byte)rawSize, (byte)0x01 }, 0, 2);
            }
            else
            {
                var rawSizeVlc = CreateVlc(rawSize);
                output.WriteByte(0);    // write Vlc base byte
                output.Write(rawSizeVlc, 0, rawSizeVlc.Length);
                output.WriteByte(0x01); // write Vlc for 0 compressedBlocks
            }

            var rawData = new byte[rawSize];
            input.Read(rawData, 0, rawSize);
            output.Write(rawData, 0, rawSize);
        }

        private void WriteBlockSizes(Stream output, int rawSize, int compressedBlocks)
        {
            byte descriptionByte = 0;

            if (rawSize > 0 && rawSize < 0x10)
                descriptionByte |= (byte)rawSize;

            if (compressedBlocks > 0 && compressedBlocks < 0x10)
                descriptionByte |= (byte)(compressedBlocks << 4);

            output.WriteByte(descriptionByte);

            if (rawSize == 0 || rawSize >= 0x10)
            {
                var rawSizeVlc = CreateVlc(rawSize);
                output.Write(rawSizeVlc, 0, rawSizeVlc.Length);
            }

            if (compressedBlocks == 0 || compressedBlocks >= 0x10)
            {
                var compressedBlocksVlc = CreateVlc(compressedBlocks);
                output.Write(compressedBlocksVlc, 0, compressedBlocksVlc.Length);
            }
        }

        private void WriteBlocks(Stream input, Stream output, int rawSize, Span<Match> matches)
        {
            // Writing raw data
            var rawData = new byte[rawSize];
            input.Read(rawData, 0, rawSize);
            output.Write(rawData, 0, rawData.Length);

            // Writing compressed data
            foreach (var match in matches)
            {
                var length = match.Length - 1;  // Length is always >0; therefore this specification stores the _length reduced by 1
                var displacementVlc = CreateVlc((int)match.Displacement, 3);

                // Variable _length encode _length and displacement

                // Write description byte
                byte descriptionByte = 0;

                descriptionByte |= displacementVlc[0];  // the displacement is the only value vlc encoded differently
                                                        // the 3 MSB are always encoded in this description block

                if (length > 0 && length < 0x10)
                    descriptionByte |= (byte)(length << 4);

                output.WriteByte(descriptionByte);

                // Write left over displacement value
                output.Write(displacementVlc, 1, displacementVlc.Length - 1);

                if (length == 0 || length >= 0x10)
                {
                    var lengthVlc = CreateVlc((int)length);
                    output.Write(lengthVlc, 0, lengthVlc.Length);
                }

                input.Position += match.Length;
            }
        }

        #region Helper

        private byte[] CreateVlc(int value, int maxBitsInMsb = 7)
        {
            var bitCount = GetBitCount(value);
            var valueLength = bitCount / 7 + (bitCount % 7 <= maxBitsInMsb ? 1 : 2);
            var returnValue = new byte[valueLength];

            for (var i = 0; i < valueLength; i++)
            {
                var partialShift = i == valueLength-1 ? maxBitsInMsb : 7;
                byte partialValue = (byte)((value & ((1 << partialShift) - 1)) << 1);
                returnValue[valueLength - 1 - i] = partialValue;
                value >>= 7;
            }

            returnValue[valueLength - 1] |= 0x1;
            return returnValue;
        }

        private static int GetBitCount(long value)
        {
            var bitCount = 0;
            while (value > 0)
            {
                bitCount++;
                value >>= 1;
            }

            return bitCount;
        }

        #endregion

        public void Dispose()
        {
            // nothing to dispose
        }
    }
}
