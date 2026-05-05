using Kompression.Contract.Configuration;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder;
using Kompression.Encoder.LempelZiv.PriceCalculators;

namespace Kompression.Encoder
{
    public class LzEncEncoder : ILempelZivEncoder
    {
        internal class Block
        {
            public bool InitialRead = true;
            public byte CodeByte;
            public long CodeBytePosition;
            public long MatchEndPosition;
        }

        public void Configure(ILempelZivEncoderOptionsBuilder matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new LzEncPriceCalculator())
                .FindPatternMatches().WithinLimitations(3, -1, 1, 0xBFFF);
        }

        public void Encode(Stream input, Stream output, IEnumerable<LempelZivMatch> matches)
        {
            var block = new Block();

            foreach (var match in matches.ToArray())
            {
                if (input.Position < match.Position)
                    WriteRawData(input, output, block, match.Position - input.Position);

                if (block.InitialRead)
                    block.InitialRead = false;

                WriteMatchData(input, output, block, match);
            }

            if (input.Position < input.Length)
                WriteRawData(input, output, block, input.Length - input.Position);

            // Write ending match flag
            output.WriteByte(0x11);
            output.WriteByte(0);
            output.WriteByte(0);
        }

        private static void WriteRawData(Stream input, Stream output, Block block, long length)
        {
            if (block.InitialRead)
            {
                // Apply special rules for first raw data read
                if (length <= 0xee)
                {
                    output.WriteByte((byte)(length + 0x11));
                }
                else
                {
                    output.WriteByte(0);
                    output.Write(EncodeLength(length - 3, 4));
                }
            }
            else
            {
                if (length <= 3)
                {
                    block.CodeByte |= (byte)length;

                    output.Position = block.CodeBytePosition;
                    output.WriteByte(block.CodeByte);

                    output.Position = block.MatchEndPosition;
                }
                else
                {
                    if (length <= 0x12)
                    {
                        output.WriteByte((byte)(length - 3));
                    }
                    else
                    {
                        output.WriteByte(0);
                        output.Write(EncodeLength(length - 3, 4));
                    }
                }
            }

            for (var i = 0; i < length; i++)
                output.WriteByte((byte)input.ReadByte());
        }

        private static void WriteMatchData(Stream input, Stream output, Block block, LempelZivMatch lempelZivMatch)
        {
            if (lempelZivMatch.Displacement <= 0x4000)
            {
                // Write encoded matchLength
                var localCode = (byte)0x20;
                var length = lempelZivMatch.Length - 2;
                if (length <= 0x1F)
                    localCode |= (byte)length;

                output.WriteByte(localCode);
                if (length > 0x1F)
                    output.Write(EncodeLength(length, 5));

                // Remember positions for later edit in raw data write
                block.CodeBytePosition = output.Position;
                block.MatchEndPosition = output.Position + 2;

                // Write encoded displacement
                block.CodeByte = (byte)(lempelZivMatch.Displacement - 1 << 2);
                var byte2 = (byte)(lempelZivMatch.Displacement - 1 >> 6);

                output.WriteByte(block.CodeByte);
                output.WriteByte(byte2);
            }
            else
            {
                // Write encoded matchLength
                var localCode = (byte)0x10;
                var length = lempelZivMatch.Length - 2;
                if (length <= 0x7)
                    localCode |= (byte)length;
                if (lempelZivMatch.Displacement >= 0x8000)
                    localCode |= 0x8;

                output.WriteByte(localCode);
                if (length > 0x7)
                    output.Write(EncodeLength(length, 3));

                // Remember positions for later edit in raw data write
                block.CodeBytePosition = output.Position;
                block.MatchEndPosition = output.Position + 2;

                // Write encoded displacement
                block.CodeByte = (byte)(lempelZivMatch.Displacement << 2);
                var byte2 = (byte)(lempelZivMatch.Displacement >> 6);

                output.WriteByte(block.CodeByte);
                output.WriteByte(byte2);
            }

            input.Position += lempelZivMatch.Length;
        }

        private static byte[] EncodeLength(long length, int bitCount)
        {
            var bitValue = (1 << bitCount) - 1;
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(length, bitValue);

            length -= bitValue;
            var fullBytes = length / 0xFF;
            var remainder = (byte)(length - fullBytes * 0xFF);
            var result = new byte[fullBytes + (remainder > 0 ? 1 : 0)];

            result[^1] = remainder > 0 ? remainder : (byte)0xFF;

            return result;
        }
    }
}
