using System;
using System.IO;
using Kompression.PatternMatch;

namespace Kompression.Implementations.Encoders
{
    class TaikoLz80Encoder : IPatternMatchEncoder
    {
        public void Encode(Stream input, Stream output, Match[] matches)
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

            /*var length = ((code >> 4) & 0x3) + 2;
            var displacement = (code & 0xF) + 1;*/
            if (match.Displacement <= 0x10 && match.Length <= 0x5)
            {
                code = 0x40;
                code |= ((int)match.Length - 2) << 4;
                code |= (int)match.Displacement - 1;

                output.WriteByte((byte)code);
                input.Position += match.Length;

                return;
            }

            /*var length = ((code >> 2) & 0xF) + 3;
            var displacement = (((code & 0x3) << 8) | byte1) + 1;*/
            if (match.Displacement <= 0x400 && match.Length <= 0x12)
            {
                code = 0x80;
                code |= ((int)match.Length - 3) << 2;
                code |= ((int)match.Displacement - 1) >> 8;

                output.WriteByte((byte)code);
                output.WriteByte((byte)(match.Displacement - 1));
                input.Position += match.Length;

                return;
            }

            /*var length = (((code & 0x3F) << 1) | (byte1 >> 7)) + 4;
            var displacement = (((byte1 & 0x7F) << 8) | byte2) + 1;*/
            code = 0xC0;
            code |= ((int)match.Length - 4) >> 1;
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
