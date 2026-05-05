using Komponent.IO;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.InternalContract.SlimeMoriMori.ValueWriter;

namespace Kompression.Specialized.SlimeMoriMori.Encoder
{
    internal class SlimeMode5Encoder(IValueWriter valueWriter) : SlimeEncoder
    {
        public override void Encode(Stream input, BinaryBitWriter bw, LempelZivMatch[] matches)
        {
            CreateDisplacementTable([.. matches.Select(x => x.Displacement)], 2);
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

        private void WriteRawData(Stream input, BinaryBitWriter bw, long rawLength)
        {
            for (var i = rawLength; i > 0; i -= 0x40)
            {
                var partLength = Math.Min(0x40, i);
                bw.WriteBits(2, 2);
                bw.WriteBits((int)partLength - 1, 6);

                for (var j = 0; j < partLength; j++)
                    valueWriter.WriteValue(bw, (byte)input.ReadByte());
            }
        }

        private void WriteMatchData(Stream input, BinaryBitWriter bw, LempelZivMatch lempelZivMatch)
        {
            if (lempelZivMatch.Displacement == 0)
            {
                // RLE
                bw.WriteBits(3, 2);
                // Subtract 2 from length; 1 due to decoding specification and
                // another one since the match starts at displacement 0 instead of 1 as per decoder specification
                bw.WriteBits(lempelZivMatch.Length - 2, 6);
                bw.WriteByte(input.ReadByte());

                // Go back 1, to not throw off the match jumping
                input.Position--;
            }
            else
            {
                // LZ
                var dispIndex = GetDisplacementIndex(lempelZivMatch.Displacement);
                var entry = GetDisplacementEntry(dispIndex);

                bw.WriteBits(dispIndex, 2);
                bw.WriteBits(lempelZivMatch.Displacement - entry.DisplacementStart, entry.ReadBits);
                bw.WriteBits(lempelZivMatch.Length - 3, 6);
            }
        }
    }
}
