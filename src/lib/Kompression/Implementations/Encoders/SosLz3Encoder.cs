using System;
using System.Collections.Generic;
using System.IO;
using Kompression.Implementations.PriceCalculators;
using Kontract.Kompression.Interfaces.Configuration;
using Kontract.Kompression.Models.PatternMatch;

namespace Kompression.Implementations.Encoders
{
    class SosLz3Encoder : ILzEncoder
    {
        public void Configure(IInternalMatchOptions matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new SosLz3PriceCalculator()).FindMatches().WithinLimitations(4, 100, 1, 0xFFFF);
        }

        public void Encode(Stream input, Stream output, IEnumerable<Match> matches)
        {
            var lastMatchPosition = 0;
            foreach (var match in matches)
            {
                var literalLength = match.Position - lastMatchPosition;
                var matchLength = match.Length - 4;

                // Write flag block
                var flag = Math.Min(15, literalLength) << 4 | Math.Min(15, matchLength);
                output.WriteByte((byte)flag);

                // Write remaining literal length
                var remainingLiteralLength = literalLength - 15;
                while (remainingLiteralLength > 0)
                {
                    output.WriteByte((byte)Math.Min(255, remainingLiteralLength));
                    remainingLiteralLength -= 255;
                }

                // Write literal data
                var literalBuffer = new byte[literalLength];
                input.Read(literalBuffer);
                output.Write(literalBuffer);

                // Write match offset
                output.WriteByte((byte)(match.Displacement >> 8));
                output.WriteByte((byte)match.Displacement);

                // Write remaining match length
                var remainingMatchLength = matchLength - 15;
                while (remainingMatchLength > 0)
                {
                    output.WriteByte((byte)Math.Min(255, remainingMatchLength));
                    remainingMatchLength -= 255;
                }

                lastMatchPosition = match.Position;
            }
        }
    }
}
