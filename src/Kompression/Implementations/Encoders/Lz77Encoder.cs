using System.Collections.Generic;
using System.IO;
using Komponent.IO;
using Kompression.Implementations.PriceCalculators;
using Kontract.Kompression.Configuration;
using Kontract.Kompression.Model.PatternMatch;
using Kontract.Models.IO;

namespace Kompression.Implementations.Encoders
{
    // TODO: Test this compression thoroughly
    public class Lz77Encoder : ILzEncoder
    {
        public void Configure(IInternalMatchOptions matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new Lz77PriceCalculator())
                .FindMatches().WithinLimitations(1, 0xFF, 1, 0xFF)
                .SkipUnitsAfterMatch(1);
        }

        public void Encode(Stream input, Stream output, IEnumerable<Match> matches)
        {
            WriteCompressedData(input, output, matches);
        }

        private void WriteCompressedData(Stream input, Stream output, IEnumerable<Match> matches)
        {
            using var bw = new BitWriter(output, BitOrder.LeastSignificantBitFirst, 1, ByteOrder.BigEndian);

            foreach (var match in matches)
            {
                while (input.Position < match.Position)
                {
                    bw.WriteBit(0);
                    bw.WriteByte((byte)input.ReadByte());
                }

                bw.WriteBit(1);
                bw.WriteByte((byte)match.Displacement);
                bw.WriteByte(match.Length);

                input.Position += match.Length;
                bw.WriteByte(input.ReadByte());
            }

            while (input.Position < input.Length)
            {
                bw.WriteBit(0);
                bw.WriteByte((byte)input.ReadByte());
            }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
