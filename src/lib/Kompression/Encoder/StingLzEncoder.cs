using System.Buffers.Binary;
using Kompression.Contract.Configuration;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder;
using Kompression.Encoder.LempelZiv.PriceCalculators;

namespace Kompression.Encoder
{
    public class StingLzEncoder : ILempelZivEncoder
    {
        public void Configure(ILempelZivEncoderOptionsBuilder matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new StingLzPriceCalculator())
                .FindPatternMatches().WithinLimitations(3, 258, 1, 255);
        }

        public void Encode(Stream input, Stream output, IEnumerable<LempelZivMatch> matches)
        {
            var matchArray = matches.ToArray();
            var tokenCount = CalculateTokenCount(input.Length, matchArray);
            var flagBufferSize = (tokenCount + 7 & ~7) / 8;

            var flagBufferOffset = 0x10;
            var tokenBufferOffset = flagBufferOffset + flagBufferSize;
            var flagBufferPosition = flagBufferOffset;
            var tokenBufferPosition = tokenBufferOffset;

            var flags = 0;
            var flagPosition = 8;

            foreach (var match in matchArray)
            {
                // Write literals to next match
                for (var i = input.Position; i < match.Position; i++)
                {
                    if (flagPosition == 0)
                    {
                        output.Position = flagBufferPosition++;
                        output.WriteByte((byte)flags);

                        flagPosition = 8;
                        flags = 0;
                    }

                    flagPosition--;

                    output.Position = tokenBufferPosition++;
                    output.WriteByte((byte)input.ReadByte());
                }

                // Write match
                if (flagPosition == 0)
                {
                    output.Position = flagBufferPosition++;
                    output.WriteByte((byte)flags);

                    flagPosition = 8;
                    flags = 0;
                }

                flags |= 1 << --flagPosition;

                output.Position = tokenBufferPosition;
                tokenBufferPosition += 2;

                output.WriteByte((byte)match.Displacement);
                output.WriteByte((byte)(match.Length - 3));

                input.Position += match.Length;
            }

            // Write remaining literals
            for (var i = input.Position; i < input.Length; i++)
            {
                if (flagPosition == 0)
                {
                    output.Position = flagBufferPosition++;
                    output.WriteByte((byte)flags);

                    flagPosition = 8;
                    flags = 0;
                }

                flagPosition--;

                output.Position = tokenBufferPosition++;
                output.WriteByte((byte)input.ReadByte());
            }

            output.Position = flagBufferPosition;
            output.WriteByte((byte)flags);

            // Write header
            WriteHeader(output, (int)input.Length, tokenCount, tokenBufferOffset);
        }

        private static void WriteHeader(Stream output, int decompressedSize, int tokenCount, int dataOffset)
        {
            output.Position = 0;
            var buffer = new byte[4];

            BinaryPrimitives.WriteUInt32BigEndian(buffer, 0x4C5A3737);
            output.Write(buffer);

            BinaryPrimitives.WriteInt32LittleEndian(buffer, decompressedSize);
            output.Write(buffer);

            BinaryPrimitives.WriteInt32LittleEndian(buffer, tokenCount);
            output.Write(buffer);

            BinaryPrimitives.WriteInt32LittleEndian(buffer, dataOffset);
            output.Write(buffer);
        }

        private static int CalculateTokenCount(long decompressedSize, IList<LempelZivMatch> matches)
        {
            var tokenCount = 0;
            var position = 0;

            foreach (var match in matches)
            {
                tokenCount += match.Position - position;
                tokenCount += 1;

                position = match.Position + match.Length;
            }

            tokenCount += (int)decompressedSize - position;

            return tokenCount;
        }
    }
}
