using System;
using System.IO;
using Kompression.Configuration;
using Kompression.Interfaces;

namespace Kompression.Implementations.Encoders
{
    public class NintendoRleEncoder : IEncoder
    {
        private byte[] _buffer;

        private IMatchParser _matchParser;

        public NintendoRleEncoder(IMatchParser matchParser)
        {
            _matchParser = matchParser;
        }

        public void Encode(Stream input, Stream output)
        {
            if (input.Length > 0xFFFFFF)
                throw new InvalidOperationException("Data to compress is too long.");

            var compressionHeader = new byte[] { 0x30, (byte)(input.Length & 0xFF), (byte)((input.Length >> 8) & 0xFF), (byte)((input.Length >> 16) & 0xFF) };
            output.Write(compressionHeader, 0, 4);

            _buffer = new byte[0x80];
            var matches = _matchParser.ParseMatches(input);
            foreach (var match in matches)
            {
                if (input.Position < match.Position)
                {
                    // If we have unmatched data before the match, create enough uncompressed blocks
                    HandleUncompressedData(input, output, (int)(match.Position - input.Position));
                }

                // Write matched data as compressed block
                var rleValue = (byte)input.ReadByte();
                HandleCompressedBlock(output, rleValue, match.Length);
                input.Position += match.Length - 1;
            }

            // If there is unmatched data left after last match, handle as uncompressed block
            if (input.Position < input.Length)
            {
                HandleUncompressedData(input, output, (int)(input.Length - input.Position));
            }
        }

        private void HandleUncompressedData(Stream input, Stream output, int dataLength)
        {
            while (dataLength > 0)
            {
                var subLength = Math.Min(dataLength, 0x80);
                input.Read(_buffer, 0, subLength);

                output.WriteByte((byte)(subLength - 1));
                output.Write(_buffer, 0, subLength);

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

        public void Dispose()
        {
            _buffer = null;
        }
    }
}
