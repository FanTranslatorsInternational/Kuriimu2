using System;
using System.IO;
using Kompression.Extensions;
using Kontract.Kompression;
using Kontract.Kompression.Configuration;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.Implementations.Encoders
{
    public class SpikeChunsoftEncoder : IEncoder
    {
        private IMatchParser _matchParser;

        public SpikeChunsoftEncoder(IMatchParser parser)
        {
            _matchParser = parser;
        }

        public void Encode(Stream input, Stream output)
        {
            output.Position += 0xC;

            var matches = _matchParser.ParseMatches(input);
            foreach (var match in matches)
            {
                if (input.Position < match.Position)
                    WriteRawData(input, output, match.Position - input.Position);

                WriteMatchData(input, output, match);
            }

            if (input.Position < input.Length)
                WriteRawData(input, output, input.Length - input.Position);

            WriteHeaderData(output, input.Length);
        }

        private void WriteRawData(Stream input, Stream output, long length)
        {
            while (length > 0)
            {
                var cappedLength = Math.Min(length, 0x1FFF);
                if (cappedLength <= 0x1F)
                    output.WriteByte((byte)cappedLength);
                else
                {
                    output.WriteByte((byte)(0x20 | (cappedLength >> 8)));
                    output.WriteByte((byte)cappedLength);
                }

                for (var i = 0; i < cappedLength; i++)
                    output.WriteByte((byte)input.ReadByte());

                length -= cappedLength;
            }
        }

        private void WriteMatchData(Stream input, Stream output, Match match)
        {
            var length = match.Length - 4;
            if (match.Displacement == 0)
            {
                // Rle
                if (length <= 0xF)
                    output.WriteByte((byte)(0x40 | length));
                else
                {
                    output.WriteByte((byte)(0x50 | (length >> 8)));
                    output.WriteByte((byte)length);
                }

                output.WriteByte((byte)input.ReadByte());
                input.Position--;
            }
            else
            {
                // Lz

                // Write displacement part first
                var cappedLength = Math.Min(length, 3);

                output.WriteByte((byte)(0x80 | (cappedLength << 5) | (match.Displacement >> 8)));
                output.WriteByte((byte)match.Displacement);

                length -= cappedLength;
                while (length > 0)
                {
                    cappedLength = Math.Min(length, 0x1F);

                    output.WriteByte((byte)(0x60 | cappedLength));

                    length -= cappedLength;
                }
            }

            input.Position += match.Length;
        }

        private void WriteHeaderData(Stream output, long uncompressedLength)
        {
            var endPosition = output.Position;
            output.Position = 0;

            var magic = new byte[] { 0xFC, 0xAA, 0x55, 0xA7 };
            Write(output,magic);
            Write(output, ((int)uncompressedLength).GetArrayLittleEndian());
            Write(output, ((int)output.Length).GetArrayLittleEndian());

            output.Position = endPosition;
        }

        private void Write(Stream output, byte[] data)
        {
#if NET_CORE_31
            output.Write(data);
#else
            output.Write(data,0,data.Length);
#endif
        }

        public void Dispose()
        {
            _matchParser?.Dispose();
            _matchParser = null;
        }
    }
}
