using System;
using System.Collections.Generic;
using System.IO;
using Kompression.Implementations.PriceCalculators;
using Kontract.Kompression.Configuration;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.Implementations.Encoders.Headerless
{
    public class RleHeaderlessEncoder
    {
        public void Configure(IInternalMatchOptions matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new NintendoRlePriceCalculator())
                .FindMatches().WithinLimitations(3, 0x82);
        }

        public void Encode(Stream input, Stream output, IEnumerable<Match> matches)
        {
            var buffer = new byte[0x80];
            foreach (var match in matches)
            {
                if (input.Position < match.Position)
                {
                    // If we have unmatched data before the match, create enough uncompressed blocks
                    HandleUncompressedData(input, output, buffer, (int)(match.Position - input.Position));
                }

                // Write matched data as compressed block
                var rleValue = (byte)input.ReadByte();
                HandleCompressedBlock(output, rleValue, match.Length);
                input.Position += match.Length - 1;
            }

            // If there is unmatched data left after last match, handle as uncompressed block
            if (input.Position < input.Length)
            {
                HandleUncompressedData(input, output, buffer, (int)(input.Length - input.Position));
            }
        }

        private void HandleUncompressedData(Stream input, Stream output, byte[] buffer, int dataLength)
        {
            while (dataLength > 0)
            {
                var subLength = Math.Min(dataLength, 0x80);
                input.Read(buffer, 0, subLength);

                output.WriteByte((byte)(subLength - 1));
                output.Write(buffer, 0, subLength);

                dataLength -= subLength;
            }
        }

        private void HandleCompressedBlock(Stream output, byte value, int repetition)
        {
            while (repetition > 0)
            {
                var subLength = Math.Min(repetition, 0x82);

                output.WriteByte((byte)(0x80 | (repetition - 3)));
                output.WriteByte(value);

                repetition -= subLength;
            }
        }
    }
}
