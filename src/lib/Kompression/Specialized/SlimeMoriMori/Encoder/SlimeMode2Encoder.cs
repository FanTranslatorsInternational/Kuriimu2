using Komponent.IO;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.InternalContract.SlimeMoriMori.ValueWriter;

namespace Kompression.Specialized.SlimeMoriMori.Encoder
{
    internal class SlimeMode2Encoder(IValueWriter valueWriter) : SlimeEncoder
    {
        public override void Encode(Stream input, BinaryBitWriter bw, LempelZivMatch[] matches)
        {
            CreateDisplacementTable([.. matches.Select(x => x.Displacement)], 7);
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

        private void WriteRawData(Stream input, BinaryBitWriter bw, long rawLength)
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
                    valueWriter.WriteValue(bw, (byte)input.ReadByte());
            }
            else
            {
                // Write raw data as 1 flag bit and 1 huffman value continuously
                for (var i = 0; i < rawLength; i++)
                {
                    bw.WriteBit(0);
                    valueWriter.WriteValue(bw, (byte)input.ReadByte());
                }
            }
        }

        private void WriteMatchData(BinaryBitWriter bw, LempelZivMatch lempelZivMatch)
        {
            bw.WriteBit(1);
            var dispIndex = GetDisplacementIndex(lempelZivMatch.Displacement);
            var entry = GetDisplacementEntry(dispIndex);

            if (lempelZivMatch.Length <= 18)
            {
                bw.WriteBits(dispIndex, 3);
                bw.WriteBits(lempelZivMatch.Displacement - entry.DisplacementStart, entry.ReadBits);
                bw.WriteBits(lempelZivMatch.Length - 3, 4);
            }
            else
            {
                bw.WriteBits(0x7, 3);

                var vleBits = GetVleBitCount((lempelZivMatch.Length - 3) >> 4);
                WriteVleValue(bw, (lempelZivMatch.Length - 3) >> 4, vleBits);

                bw.WriteBit(1);
                bw.WriteBits(dispIndex, 3);
                bw.WriteBits(lempelZivMatch.Displacement - entry.DisplacementStart, entry.ReadBits);
                bw.WriteBits((lempelZivMatch.Length - 3) & 0xF, 4);
            }
        }

        private static int GetVleBitCount(int value)
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

        private static void WriteVleValue(BinaryBitWriter bw, int value, int vleBits)
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
