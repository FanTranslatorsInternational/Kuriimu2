using System.IO;
using Kompression.LempelZiv.Matcher;
using Kompression.LempelZiv.Matcher.Models;
using Kompression.LempelZiv.Models;

namespace Kompression.LempelZiv.Encoders
{
    class LzssVlcEncoder : ILzEncoder
    {
        private readonly ILzMatcher _matcher;

        public LzssVlcEncoder(ILzMatcher matcher)
        {
            _matcher = matcher;
        }

        public void Encode(Stream input, Stream output)
        {
            var decompressedSize = CreateVlc((int)input.Length);
            var unk1 = CreateVlc(0x19);
            var unk2 = CreateVlc(0);

            output.Write(decompressedSize, 0, decompressedSize.Length);
            output.Write(unk1, 0, unk1.Length);
            output.Write(unk2, 0, unk2.Length);

            var matches = _matcher.FindMatches(input);

            WriteCompressedData(input, output, matches);
        }

        private void WriteCompressedData(Stream input, Stream output, LzMatch[] matches)
        {
            var lzIndex = 0;
            while (input.Position < input.Length)
            {
                if (lzIndex >= matches.Length)
                {
                    // If we have still uncompressed data, but no compressed blocks follow

                    var rawSize = (int)(input.Length - input.Position);
                    if (rawSize > 0 && rawSize < 0x10)
                    {
                        output.Write(new[] { (byte)rawSize, (byte)0x01 }, 0, 2);
                    }
                    else
                    {
                        var rawSizeVlc = CreateVlc(rawSize);
                        output.WriteByte(0);
                        output.Write(rawSizeVlc, 0, rawSizeVlc.Length);
                        output.WriteByte(0x01);
                    }

                    var rawData = new byte[rawSize];
                    input.Read(rawData, 0, rawSize);
                    output.Write(rawData, 0, rawSize);
                }
                else
                {
                    // If we have uncompressed data followed by n compressed blocks

                    // Variable length encode compressed block count and raw data size
                    var rawSize = (int)(matches[lzIndex].Position - input.Position);

                    var compressedBlocks = 0;
                    var positionOffset = 0;
                    while (lzIndex + compressedBlocks < matches.Length &&
                           matches[lzIndex + compressedBlocks].Position == input.Position + rawSize + positionOffset)
                    {
                        positionOffset += matches[lzIndex + compressedBlocks].Length;
                        compressedBlocks++;
                    }

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
                        var compressedBlocksVlc = CreateVlc(compressedBlocks, 3);
                        output.Write(compressedBlocksVlc, 0, compressedBlocksVlc.Length);
                    }

                    // Writing raw data
                    var rawData = new byte[rawSize];
                    input.Read(rawData, 0, rawSize);
                    output.Write(rawData, 0, rawData.Length);

                    // Writing compressed data now
                    for (var i = 0; i < compressedBlocks; i++)
                    {
                        var length = matches[lzIndex + i].Length - 1;
                        var displacementVlc = CreateVlc((int)matches[lzIndex + i].Displacement, 3);

                        // Variable length encode length and displacement
                        descriptionByte = 0;
                        if (length > 0 && length < 0x10)
                            descriptionByte |= (byte)(length << 4);
                        descriptionByte |= displacementVlc[0];
                        output.WriteByte(descriptionByte);

                        output.Write(displacementVlc, 1, displacementVlc.Length - 1);
                        if (length == 0 || length >= 0x10)
                        {
                            var lengthVlc = CreateVlc(length);
                            output.Write(lengthVlc, 0, lengthVlc.Length);
                        }

                        input.Position += matches[lzIndex + i].Length;
                    }

                    lzIndex += compressedBlocks;
                }
            }
        }

        private byte[] CreateVlc(int value, int maxBitsInMsb = 7)
        {
            int bitCount = GetBitCount(value);
            var returnValue = new byte[bitCount / 7 + (bitCount % 7 <= maxBitsInMsb ? 1 : 2)];

            for (var i = 0; i < returnValue.Length; i++)
            {
                var partialShift = i == returnValue.Length - 1 ? maxBitsInMsb : 7;
                byte partialValue = (byte)((value & ((1 << partialShift) - 1)) << 1);
                returnValue[returnValue.Length - 1 - i] = partialValue;
                value >>= 7;
            }

            returnValue[returnValue.Length - 1] |= 0x1;
            return returnValue;
        }

        private static int GetBitCount(int value)
        {
            int bitCount = 0;
            while (value > 0)
            {
                bitCount++;
                value >>= 1;
            }

            return bitCount;
        }

        #region Dispose

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool dispose)
        {
            if (dispose)
                _matcher.Dispose();
        }

        #endregion
    }
}
