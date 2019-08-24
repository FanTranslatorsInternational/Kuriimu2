using System.IO;
using System.Linq;
using Kompression.IO;
using Kompression.Specialized.SlimeMoriMori.ValueWriters;

namespace Kompression.Specialized.SlimeMoriMori.Encoders
{
    class SlimeMode1Encoder : SlimeEncoder
    {
        private IValueWriter _valueWriter;

        public SlimeMode1Encoder(IValueWriter valueWriter)
        {
            _valueWriter = valueWriter;
        }

        public override void Encode(Stream input, BitWriter bw, Match[] matches)
        {
            CreateDisplacementTable(matches.Select(x => x.Displacement).ToArray(), 4);
            WriteDisplacementTable(bw);

            foreach (var match in matches)
            {
                var rawLength = match.Position - input.Position;
                if (rawLength > 0)
                    WriteRawData(input, bw, rawLength);

                WriteMatchData(bw, match);
                input.Position += match.Length;
            }

            if (input.Length - input.Position > 0)
                WriteRawData(input, bw, input.Length - input.Position);
        }

        private void WriteRawData(Stream input, BitWriter bw, long rawLength)
        {
            for (var i = 0; i < rawLength; i++)
            {
                bw.WriteBit(0);
                _valueWriter.WriteValue(bw, (byte)input.ReadByte());
            }
        }

        private void WriteMatchData(BitWriter bw, Match match)
        {
            bw.WriteBit(1);

            var dispIndex = GetDisplacementIndex(match.Displacement);
            var entry = GetDisplacementEntry(dispIndex);

            bw.WriteBits(dispIndex, 2);
            bw.WriteBits((int)match.Displacement - entry.DisplacementStart, entry.ReadBits);
            bw.WriteBits((int)match.Length - 3, 4);
        }
    }
}
