using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kompression.Implementations.PriceCalculators;
using Kontract.Kompression.Configuration;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.Implementations.Encoders
{
    public class StingLzEncoder : ILzEncoder
    {
        public void Configure(IInternalMatchOptions matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new StingLzPriceCalculator())
                .FindMatches().WithinLimitations(3, 258, 1, 255);
        }

        public void Encode(Stream input, Stream output, IEnumerable<Match> matches)
        {
            var matchArray = matches.ToArray();
            var tokenCount = CalculateTokenCount(input.Length, matchArray);
            var flagBufferSize = ((tokenCount + 7) & ~7) / 8;

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

        private void WriteHeader(Stream output, int decompressedSize, int tokenCount, int dataOffset)
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

        private int CalculateTokenCount(long decompressedSize, IList<Match> matches)
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
