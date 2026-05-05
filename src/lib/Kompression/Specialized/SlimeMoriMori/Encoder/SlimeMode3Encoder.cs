using Komponent.IO;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.InternalContract.SlimeMoriMori.ValueWriter;

namespace Kompression.Specialized.SlimeMoriMori.Encoder
{
    internal class SlimeMode3Encoder(IValueWriter valueWriter) : SlimeEncoder
    {
        public override void Encode(Stream input, BinaryBitWriter bw, LempelZivMatch[] matches)
        {
            CreateDisplacementTable([.. matches.Select(x => x.Displacement >> 1)], 3);
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
                    valueWriter.WriteValue(bw, (byte)input.ReadByte());
                    valueWriter.WriteValue(bw, (byte)input.ReadByte());
                }
            }
            else
            {
                // Write raw data as 1 flag bit and 2 huffman value continuously
                for (var i = 0; i < rawLength; i++)
                {
                    bw.WriteBit(0);
                    valueWriter.WriteValue(bw, (byte)input.ReadByte());
                    valueWriter.WriteValue(bw, (byte)input.ReadByte());
                }
            }
        }

        private void WriteMatchData(BinaryBitWriter bw, LempelZivMatch lempelZivMatch)
        {
            var displacement = lempelZivMatch.Displacement >> 1;
            var length = lempelZivMatch.Length >> 1;

            bw.WriteBit(1);
            var dispIndex = GetDisplacementIndex(displacement);
            var entry = GetDisplacementEntry(dispIndex);

            if (lempelZivMatch.Length <= 18)
            {
                bw.WriteBits(dispIndex, 2);
                bw.WriteBits(displacement - entry.DisplacementStart, entry.ReadBits);
                bw.WriteBits(length - 2, 3);
            }
            else
            {
                bw.WriteBits(0x3, 2);

                var vleBits = GetVleBitCount((length - 2) >> 3);
                WriteVleValue(bw, (length - 2) >> 3, vleBits);

                bw.WriteBit(1);
                bw.WriteBits(dispIndex, 2);
                bw.WriteBits(displacement - entry.DisplacementStart, entry.ReadBits);
                bw.WriteBits((length - 2) & 0x7, 3);
            }
        }

        private static int GetVleBitCount(int value)
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

        private static void WriteVleValue(BinaryBitWriter bw, int value, int vleBits)
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
