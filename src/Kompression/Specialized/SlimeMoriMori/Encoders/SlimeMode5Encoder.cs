using System;
using System.IO;
using System.Linq;
using Kompression.IO;
using Kompression.Specialized.SlimeMoriMori.ValueWriters;

namespace Kompression.Specialized.SlimeMoriMori.Encoders
{
    class SlimeMode5Encoder : SlimeEncoder
    {
        private IValueWriter _valueWriter;

        public SlimeMode5Encoder(IValueWriter valueWriter)
        {
            _valueWriter = valueWriter;
        }

        public override void Encode(Stream input, BitWriter bw, Match[] matches)
        {
            CreateDisplacementTable(matches.Select(x => x.Displacement).ToArray(), 2);
            WriteDisplacementTable(bw);

            foreach (var match in matches)
            {
                var rawLength = match.Position - input.Position;
                if (rawLength > 0)
                    WriteRawData(input, bw, rawLength);

                WriteMatchData(input, bw, match);
                input.Position += match.Length;
            }

            if (input.Length - input.Position > 0)
                WriteRawData(input, bw, input.Length - input.Position);
        }

        private void WriteRawData(Stream input, BitWriter bw, long rawLength)
        {
            for (var i = rawLength; i > 0; i -= 0x40)
            {
                var partLength = Math.Min(0x40, i);
                bw.WriteBits(2, 2);
                bw.WriteBits((int)partLength - 1, 6);

                for (var j = 0; j < partLength; j++)
                    _valueWriter.WriteValue(bw, (byte)input.ReadByte());
            }
        }

        private void WriteMatchData(Stream input, BitWriter bw, Match match)
        {
            if (match.Displacement == 0)
            {
                // RLE
                bw.WriteBits(3, 2);
                // Subtract 2 from length; 1 due to decoding specification and
                // another one since the match starts at displacement 0 instead of 1 as per decoder specification
                bw.WriteBits((int)match.Length - 2, 6);
                bw.WriteByte(input.ReadByte());

                // Go back 1, to not throw off the match jumping
                input.Position--;
            }
            else
            {
                // LZ
                var dispIndex = GetDisplacementIndex(match.Displacement);
                var entry = GetDisplacementEntry(dispIndex);

                bw.WriteBits(dispIndex, 2);
                bw.WriteBits((int)match.Displacement - entry.DisplacementStart, entry.ReadBits);
                bw.WriteBits((int)match.Length - 3, 6);
            }
        }
    }
}
