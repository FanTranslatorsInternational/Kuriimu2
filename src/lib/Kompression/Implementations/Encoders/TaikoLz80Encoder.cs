using System;
using System.Collections.Generic;
using System.IO;

using Kompression.Implementations.PriceCalculators;
using Kontract.Kompression.Interfaces.Configuration;
using Kontract.Kompression.Models.PatternMatch;

namespace Kompression.Implementations.Encoders
{
    public class TaikoLz80Encoder : ILzEncoder
    {
        public void Configure(IInternalMatchOptions matchOptions)
        {
	        matchOptions.CalculatePricesWith(() => new TaikoLz80PriceCalculator())
		        .FindMatches().WithinLimitations(2, 5, 1, 0x10)
		        .AndFindMatches().WithinLimitations(3, 0x12, 1, 0x400)
		        .AndFindMatches().WithinLimitations(4, 0x83, 1, 0x8000);
        }

        public void Encode(Stream input, Stream output, IEnumerable<Match> matches)
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

        private void CompressRawData(Stream input, Stream output, int rawLength)
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

        private void CompressMatchData(Stream input, Stream output, Match match)
        {
            int code;

            if (match.Displacement <= 0x10 && match.Length <= 0x5)
            {
                code = 0x40;
                code |= (match.Length - 2) << 4;
                code |= match.Displacement - 1;

                output.WriteByte((byte)code);
                input.Position += match.Length;

                return;
            }

            if (match.Displacement <= 0x400 && match.Length <= 0x12)
            {
                code = 0x80;
                code |= (match.Length - 3) << 2;
                code |= (match.Displacement - 1) >> 8;

                output.WriteByte((byte)code);
                output.WriteByte((byte)(match.Displacement - 1));
                input.Position += match.Length;

                return;
            }

            code = 0xC0;
            code |= (match.Length - 4) >> 1;
            var byte1 = ((match.Length - 4) & 0x1) << 7;
            byte1 |= (match.Displacement - 1) >> 8;

            output.WriteByte((byte)code);
            output.WriteByte((byte)byte1);
            output.WriteByte((byte)(match.Displacement - 1));

            input.Position += match.Length;
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
