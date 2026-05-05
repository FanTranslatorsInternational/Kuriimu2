using Komponent.Contract.Enums;
using Komponent.IO;
using Kompression.Contract.Configuration;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder;
using Kompression.Encoder.LempelZiv.PriceCalculators;

namespace Kompression.Encoder
{
    public class Lz77Encoder : ILempelZivEncoder
    {
        public void Configure(ILempelZivEncoderOptionsBuilder matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new Lz77PriceCalculator())
                .FindPatternMatches().WithinLimitations(1, 0xFF, 1, 0xFF)
                .SkipUnitsAfterMatch(1);
        }

        public void Encode(Stream input, Stream output, IEnumerable<LempelZivMatch> matches)
        {
            WriteCompressedData(input, output, matches);
        }

        private static void WriteCompressedData(Stream input, Stream output, IEnumerable<LempelZivMatch> matches)
        {
            using var bw = new BinaryBitWriter(output, BitOrder.LeastSignificantBitFirst, 1, ByteOrder.BigEndian);

            foreach (LempelZivMatch match in matches)
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
    }
}
