using System.Collections.Generic;
using System.IO;
using Kompression.LempelZiv.Occurrence;
using Kompression.LempelZiv.Occurrence.Models;

/* Used in [PS2 Game] and MTV archive */
// TODO: Find out that PS2 game from IcySon55

namespace Kompression.LempelZiv
{
    public static class LZSSVLC
    {
        public static void Decompress(Stream input, Stream output)
        {
            var decompressedSize = ReadVlc(input);
            var unk1 = ReadVlc(input); // filetype maybe???
            var unk2 = ReadVlc(input); // compression type = 1 (LZSS?)

            while (output.Position < decompressedSize)
            {
                var value = input.ReadByte();
                var size = (value & 0xF) > 0 ? value & 0xF : ReadVlc(input);
                var compressedBlocks = value >> 4 > 0 ? value >> 4 : ReadVlc(input);
                for (var i = 0; i < size; i++)
                    output.WriteByte((byte)input.ReadByte());

                for (var i = 0; i < compressedBlocks; i++)
                {
                    value = input.ReadByte();
                    var offset = ReadVlc(input, value & 0xF);   // yes, this one is the only one seemingly using this scheme of reading a value
                    var length = value >> 4 > 0 ? value >> 4 : ReadVlc(input);
                    length += 1;

                    var currentPosition = output.Position;
                    var copyPosition = currentPosition - offset;
                    for (var j = 0; j < length; j++)
                    {
                        output.Position = copyPosition + j;
                        var copyValue = output.ReadByte();
                        output.Position = currentPosition + j;
                        output.WriteByte((byte)copyValue);
                    }
                }
            }
        }

        public static void Compress(Stream input, Stream output)
        {
            var decompressedSize = CreateVlc((int)input.Length);
            var unk1 = CreateVlc(0x19);
            var unk2 = CreateVlc(0);

            output.Write(decompressedSize, 0, decompressedSize.Length);
            output.Write(unk1, 0, unk1.Length);
            output.Write(unk2, 0, unk2.Length);

            var lzFinder = new LzOccurrenceFinder(LzMode.Naive, 0x1000, 4, 100110);
            var lzResults = lzFinder.Process(input)/*.OrderBy(x => x.Position).ToList()*/;

            WriteCompressedData(input, output, lzResults);
        }

        private static int ReadVlc(Stream input, int initialValue = 0)
        {
            var flag = initialValue & 0x1;
            var result = initialValue >> 1;
            if (flag == 1)
                return result;

            byte next;
            do
            {
                next = (byte)input.ReadByte();
                result <<= 7;
                result |= next >> 1;
            } while ((next & 0x1) == 0);

            return result;
        }

        private static void WriteCompressedData(Stream input, Stream output, List<LzResult> lzResults)
        {
            int lzIndex = 0;
            while (input.Position < input.Length)
            {
                if (lzIndex >= lzResults.Count)
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
                    var rawSize = (int)(lzResults[lzIndex].Position - input.Position);

                    var compressedBlocks = 0;
                    var positionOffset = 0;
                    while (lzIndex + compressedBlocks < lzResults.Count &&
                           lzResults[lzIndex + compressedBlocks].Position == input.Position + rawSize + positionOffset)
                    {
                        positionOffset += lzResults[lzIndex + compressedBlocks].Length;
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
                        var length = lzResults[lzIndex + i].Length - 1;
                        var displacementVlc = CreateVlc((int)lzResults[lzIndex + i].Displacement, 3);

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

                        input.Position += lzResults[lzIndex + i].Length;
                    }

                    lzIndex += compressedBlocks;
                }
            }
        }

        private static byte[] CreateVlc(int value, int maxBitsInMsb = 7)
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
    }
}
