using System.IO;
using System.Linq;
using Kompression.IO;
using Kompression.PatternMatch;
using Kompression.Specialized.SlimeMoriMori.ValueWriters;

namespace Kompression.Specialized.SlimeMoriMori.Encoders
{
    class SlimeMode3Encoder : SlimeEncoder
    {
        private IValueWriter _valueWriter;

        public SlimeMode3Encoder(IValueWriter valueWriter)
        {
            _valueWriter = valueWriter;
        }

        public override void Encode(Stream input, BitWriter bw, Match[] matches)
        {
            CreateDisplacementTable(matches.Select(x => x.Displacement >> 1).ToArray(), 3);
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
            rawLength >>= 1;
            var vleBits = GetVleBitCount((int)rawLength - 1);

            if (vleBits <= rawLength)
            {
                // Write raw data with length pre written
                bw.WriteBit(1);
                bw.WriteBits(0x3, 2);
                WriteVleValue(bw, (int)rawLength - 1, vleBits);
                bw.WriteBit(0);

                for (var i = 0; i < rawLength; i++)
                {
                    _valueWriter.WriteValue(bw, (byte)input.ReadByte());
                    _valueWriter.WriteValue(bw, (byte)input.ReadByte());
                }
            }
            else
            {
                // Write raw data as 1 flag bit and 2 huffman value continuously
                for (var i = 0; i < rawLength; i++)
                {
                    bw.WriteBit(0);
                    _valueWriter.WriteValue(bw, (byte)input.ReadByte());
                    _valueWriter.WriteValue(bw, (byte)input.ReadByte());
                }
            }
        }

        private void WriteMatchData(BitWriter bw, Match match)
        {
            var displacement = match.Displacement>>1;
            var length = match.Length >>1;

            bw.WriteBit(1);
            var dispIndex = GetDisplacementIndex(displacement);
            var entry = GetDisplacementEntry(dispIndex);

            if (match.Length <= 18)
            {
                bw.WriteBits(dispIndex, 2);
                bw.WriteBits((int)displacement - entry.DisplacementStart, entry.ReadBits);
                bw.WriteBits((int)length - 2, 3);
            }
            else
            {
                bw.WriteBits(0x3, 2);

                var vleBits = GetVleBitCount(((int)length - 2) >> 3);
                WriteVleValue(bw, ((int)length - 2) >> 3, vleBits);

                bw.WriteBit(1);
                bw.WriteBits(dispIndex, 2);
                bw.WriteBits((int)displacement - entry.DisplacementStart, entry.ReadBits);
                bw.WriteBits(((int)length - 2) & 0x7, 3);
            }
        }

        private int GetVleBitCount(int value)
        {
            if (value == 0)
                return 3;

            var vleBits = 0;
            while (value > 0)
            {
                vleBits += 3;
                value >>= 2;
            }

            return vleBits;
        }

        private void WriteVleValue(BitWriter bw, int value, int vleBits)
        {
            var valueBits = vleBits / 3 * 2;
            while (valueBits > 0)
            {
                valueBits -= 2;
                var valuePart = (value >> valueBits) & 0x3;
                bw.WriteBits(valuePart, 2);
                bw.WriteBit(valueBits > 0 ? 1 : 0);
            }
        }
    }
}
