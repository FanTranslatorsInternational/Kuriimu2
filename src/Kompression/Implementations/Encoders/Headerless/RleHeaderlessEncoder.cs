using System;
using System.IO;
using Kontract.Kompression;

namespace Kompression.Implementations.Encoders.Headerless
{
    public class RleHeaderlessEncoder
    {
        private readonly IMatchParser _matchParser;

        public RleHeaderlessEncoder(IMatchParser matchParser)
        {
            _matchParser = matchParser;
        }

        public void Encode(Stream input, Stream output)
        {
            var buffer = new byte[0x80];
            var matches = _matchParser.ParseMatches(input);
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
