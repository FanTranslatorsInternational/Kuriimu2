using Kompression.Contract.Configuration;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder;
using Kompression.Encoder.LempelZiv.PriceCalculators;

namespace Kompression.Encoder
{
    /* Found in SMT Nocturne on the PS2 */
    internal class PsLzEncoder : ILempelZivEncoder
    {
        public void Configure(ILempelZivEncoderOptionsBuilder matchOptions)
        {
            matchOptions.CalculatePricesWith(() => new PsLzPriceCalculator())
                .FindPatternMatches().WithinLimitations(1, 0xFFFF, 1, 0xFFFF)
                .AndFindRunLength().WithinLimitations(1, 0xFFFF)
                .AndFindConstantRunLength(0).WithinLimitations(1, 0xFFFF);
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

            output.WriteByte(0xFF);
        }

        private static void CompressRawData(Stream input, Stream output, int rawLength)
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
                _ = input.Read(buffer, 0, lengthEncode);
                output.Write(buffer, 0, lengthEncode);

                rawLength -= lengthEncode;
            }
        }

        private static void CompressMatchData(Stream input, Stream output, LempelZivMatch lempelZivMatch)
        {
            var modeByte = (byte)0;
            if (lempelZivMatch.Length <= 0x1F)
                modeByte |= (byte)lempelZivMatch.Length;

            if (lempelZivMatch.Displacement == 0)
            {
                var rleValue = (byte)input.ReadByte();
                if (rleValue == 0)
                {
                    // Encode 0 RLE match
                    modeByte |= 0x20;

                    output.WriteByte(modeByte);
                    if (lempelZivMatch.Length > 0x1F)
                        WriteInt16Le(lempelZivMatch.Length, output);
                }
                else
                {
                    // Encode variable value RLE match
                    modeByte |= 0x40;

                    output.WriteByte(modeByte);
                    if (lempelZivMatch.Length > 0x1F)
                        WriteInt16Le(lempelZivMatch.Length, output);

                    output.WriteByte(rleValue);
                }

                input.Position--;
            }
            else if (lempelZivMatch.Displacement <= 0xFF)
            {
                modeByte |= 0x60;

                output.WriteByte(modeByte);
                if (lempelZivMatch.Length > 0x1F)
                    WriteInt16Le(lempelZivMatch.Length, output);

                output.WriteByte((byte)lempelZivMatch.Displacement);
            }
            else
            {
                modeByte |= 0x80;

                output.WriteByte(modeByte);
                if (lempelZivMatch.Length > 0x1F)
                    WriteInt16Le(lempelZivMatch.Length, output);

                WriteInt16Le(lempelZivMatch.Displacement, output);
            }

            input.Position += lempelZivMatch.Length;
        }

        private static void WriteInt16Le(int value, Stream output)
        {
            output.WriteByte((byte)(value & 0xFF));
            output.WriteByte((byte)(value >> 8 & 0xFF));
        }
    }
}
