using Kompression.Contract.Configuration;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder;
using Kompression.Encoder.LempelZiv.PriceCalculators;

namespace Kompression.Encoder
{
    public class TaikoLz80Encoder : ILempelZivEncoder
    {
        public void Configure(ILempelZivEncoderOptionsBuilder matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new TaikoLz80PriceCalculator())
                .FindPatternMatches().WithinLimitations(2, 5, 1, 0x10)
                .AndFindPatternMatches().WithinLimitations(3, 0x12, 1, 0x400)
                .AndFindPatternMatches().WithinLimitations(4, 0x83, 1, 0x8000);
        }

        public void Encode(Stream input, Stream output, IEnumerable<LempelZivMatch> matches)
        {
            foreach (var match in matches)
            {
                // Compress raw data
                if (input.Position < match.Position)
                    CompressRawData(input, output, (int)(match.Position - input.Position));

                // Compress match
                CompressMatchData(input, output, match);
            }

            // Compress raw data
            if (input.Position < input.Length)
                CompressRawData(input, output, (int)(input.Length - input.Position));

            output.Write(new byte[3], 0, 3);
        }

        private static void CompressRawData(Stream input, Stream output, int rawLength)
        {
            while (rawLength > 0)
            {
                if (rawLength > 0xBF)
                {
                    var encode = Math.Min(rawLength - 0xBF, 0x7FFF);

                    output.WriteByte(0);
                    output.WriteByte((byte)(encode >> 8));
                    output.WriteByte((byte)encode);

                    for (var i = 0; i < rawLength; i++)
                        output.WriteByte((byte)input.ReadByte());

                    rawLength -= encode + 0xBF;
                }
                else if (rawLength >= 0x40)
                {
                    var encode = rawLength - 0x40;

                    output.WriteByte(0);
                    output.WriteByte((byte)(0x80 | encode));

                    for (var i = 0; i < rawLength; i++)
                        output.WriteByte((byte)input.ReadByte());

                    rawLength = 0;
                }
                else
                {
                    output.WriteByte((byte)rawLength);

                    for (var i = 0; i < rawLength; i++)
                        output.WriteByte((byte)input.ReadByte());

                    rawLength = 0;
                }
            }
        }

        private static void CompressMatchData(Stream input, Stream output, LempelZivMatch lempelZivMatch)
        {
            int code;

            if (lempelZivMatch.Displacement <= 0x10 && lempelZivMatch.Length <= 0x5)
            {
                code = 0x40;
                code |= lempelZivMatch.Length - 2 << 4;
                code |= lempelZivMatch.Displacement - 1;

                output.WriteByte((byte)code);
                input.Position += lempelZivMatch.Length;

                return;
            }

            if (lempelZivMatch.Displacement <= 0x400 && lempelZivMatch.Length <= 0x12)
            {
                code = 0x80;
                code |= lempelZivMatch.Length - 3 << 2;
                code |= lempelZivMatch.Displacement - 1 >> 8;

                output.WriteByte((byte)code);
                output.WriteByte((byte)(lempelZivMatch.Displacement - 1));
                input.Position += lempelZivMatch.Length;

                return;
            }

            code = 0xC0;
            code |= lempelZivMatch.Length - 4 >> 1;
            var byte1 = (lempelZivMatch.Length - 4 & 0x1) << 7;
            byte1 |= lempelZivMatch.Displacement - 1 >> 8;

            output.WriteByte((byte)code);
            output.WriteByte((byte)byte1);
            output.WriteByte((byte)(lempelZivMatch.Displacement - 1));

            input.Position += lempelZivMatch.Length;
        }
    }
}
