using System.IO;
using System.Linq;
using Kompression.IO;
using Kompression.LempelZiv;
using Kompression.Specialized.SlimeMoriMori.ValueWriters;

namespace Kompression.Specialized.SlimeMoriMori.Encoders
{
    class SlimeMode2Encoder : SlimeEncoder
    {
        private IValueWriter _valueWriter;

        public SlimeMode2Encoder(IValueWriter valueWriter)
        {
            _valueWriter = valueWriter;
        }

        public override void Encode(Stream input, BitWriter bw, LzMatch[] matches)
        {
            CreateDisplacementTable(matches.Select(x => x.Displacement).ToArray(), 7);
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
            var vleBits = GetVleBitCount((int)rawLength - 1);

            if (vleBits <= rawLength)
            {
                // Write raw data with length pre written
                bw.WriteBit(1);
                bw.WriteBits(0x7, 3);
                WriteVleValue(bw, (int)rawLength - 1, vleBits);
                bw.WriteBit(0);

                for (var i = 0; i < rawLength; i++)
                    _valueWriter.WriteValue(bw, (byte)input.ReadByte());
            }
            else
            {
                // Write raw data as 1 flag bit and 1 huffman value continuously
                for (var i = 0; i < rawLength; i++)
                {
                    bw.WriteBit(0);
                    _valueWriter.WriteValue(bw, (byte)input.ReadByte());
                }
            }
        }

        private void WriteMatchData(BitWriter bw, LzMatch match)
        {
            bw.WriteBit(1);
            var dispIndex = GetDisplacementIndex(match.Displacement);
            var entry = GetDisplacementEntry(dispIndex);

            if (match.Length <= 18)
            {
                bw.WriteBits(dispIndex, 3);
                bw.WriteBits((int)match.Displacement - entry.DisplacementStart, entry.ReadBits);
                bw.WriteBits(match.Length - 3, 4);
            }
            else
            {
                bw.WriteBits(0x7, 3);

                var vleBits = GetVleBitCount((match.Length - 3) >> 4);
                WriteVleValue(bw, (match.Length - 3) >> 4, vleBits);

                bw.WriteBit(1);
                bw.WriteBits(dispIndex, 3);
                bw.WriteBits((int)match.Displacement - entry.DisplacementStart, entry.ReadBits);
                bw.WriteBits((match.Length - 3) & 0xF, 4);
            }
        }

        private int GetVleBitCount(int value)
        {
            if (value == 0)
                return 4;

            var vleBits = 0;
            while (value > 0)
            {
                vleBits += 4;
                value >>= 3;
            }

            return vleBits;
        }

        private void WriteVleValue(BitWriter bw, int value, int vleBits)
        {
            var valueBits = vleBits / 4 * 3;
            while (valueBits > 0)
            {
                valueBits -= 3;
                var valuePart = (value >> valueBits) & 0x7;
                bw.WriteBits(valuePart, 3);
                bw.WriteBit(valueBits > 0 ? 1 : 0);
            }
        }
    }
}
