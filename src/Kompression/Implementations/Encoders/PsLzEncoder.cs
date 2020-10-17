using System;
using System.Collections.Generic;
using System.IO;
using Kompression.Implementations.PriceCalculators;
using Kompression.PatternMatch.MatchFinders;
using Kontract.Kompression.Configuration;
using Kontract.Kompression.Model;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.Implementations.Encoders
{
    /* Found in SMT Nocturne on the PS2 */
    class PsLzEncoder : ILzEncoder
    {
        public void Configure(IInternalMatchOptions matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new PsLzPriceCalculator())
                .FindWith((options, limits) => new HistoryMatchFinder(limits, options))
                .WithinLimitations(() => new FindLimitations(1, 0xFFFF, 1, 0xFFFF))
                .AndFindWith((options, limits) => new StaticValueRleMatchFinder(0, limits, options))
                .WithinLimitations(() => new FindLimitations(1, 0xFFFF))
                .AndFindWith((options, limits) => new RleMatchFinder(limits, options))
                .WithinLimitations(() => new FindLimitations(1, 0xFFFF));
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

            output.WriteByte(0xFF);
        }

        private void CompressRawData(Stream input, Stream output, int rawLength)
        {
            while (rawLength > 0)
            {
                var lengthEncode = Math.Min(rawLength, 0xFFFF);

                if (lengthEncode > 0x1F)
                {
                    output.WriteByte(0);
                    WriteInt16Le(lengthEncode, output);
                }
                else
                {
                    output.WriteByte((byte)lengthEncode);
                }

                var buffer = new byte[lengthEncode];
                input.Read(buffer, 0, lengthEncode);
                output.Write(buffer, 0, lengthEncode);

                rawLength -= lengthEncode;
            }
        }

        private void CompressMatchData(Stream input, Stream output, Match match)
        {
            var modeByte = (byte)0;
            if (match.Length <= 0x1F)
                modeByte |= (byte)match.Length;

            if (match.Displacement == 0)
            {
                var rleValue = (byte)input.ReadByte();
                if (rleValue == 0)
                {
                    // Encode 0 RLE match
                    modeByte |= 0x20;

                    output.WriteByte(modeByte);
                    if (match.Length > 0x1F)
                        WriteInt16Le(match.Length, output);
                }
                else
                {
                    // Encode variable value RLE match
                    modeByte |= 0x40;

                    output.WriteByte(modeByte);
                    if (match.Length > 0x1F)
                        WriteInt16Le(match.Length, output);

                    output.WriteByte(rleValue);
                }

                input.Position--;
            }
            else if (match.Displacement <= 0xFF)
            {
                modeByte |= 0x60;

                output.WriteByte(modeByte);
                if (match.Length > 0x1F)
                    WriteInt16Le(match.Length, output);

                output.WriteByte((byte)match.Displacement);
            }
            else
            {
                modeByte |= 0x80;

                output.WriteByte(modeByte);
                if (match.Length > 0x1F)
                    WriteInt16Le(match.Length, output);

                WriteInt16Le(match.Displacement, output);
            }

            input.Position += match.Length;
        }

        private void WriteInt16Le(int value, Stream output)
        {
            output.WriteByte((byte)(value & 0xFF));
            output.WriteByte((byte)((value >> 8) & 0xFF));
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
