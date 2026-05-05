using Kompression.Contract.Configuration;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder;
using Kompression.Encoder.LempelZiv.PriceCalculators;

namespace Kompression.Encoder
{
    internal class SosLz3Encoder : ILempelZivEncoder
    {
        public void Configure(ILempelZivEncoderOptionsBuilder matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new SosLz3PriceCalculator()).FindPatternMatches().WithinLimitations(4, 100, 1, 0xFFFF);
        }

        public void Encode(Stream input, Stream output, IEnumerable<LempelZivMatch> matches)
        {
            var lastMatchPosition = 0;
            foreach (LempelZivMatch match in matches)
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
