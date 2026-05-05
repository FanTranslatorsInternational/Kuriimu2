using Komponent.IO;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.InternalContract.SlimeMoriMori.ValueWriter;

namespace Kompression.Specialized.SlimeMoriMori.Encoder
{
    internal class SlimeMode1Encoder(IValueWriter valueWriter) : SlimeEncoder
    {
        public override void Encode(Stream input, BinaryBitWriter bw, LempelZivMatch[] matches)
        {
            CreateDisplacementTable([.. matches.Select(x => x.Displacement)], 4);
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
            for (var i = 0; i < rawLength; i++)
            {
                bw.WriteBit(0);
                valueWriter.WriteValue(bw, (byte)input.ReadByte());
            }
        }

        private void WriteMatchData(BinaryBitWriter bw, LempelZivMatch lempelZivMatch)
        {
            bw.WriteBit(1);

            var dispIndex = GetDisplacementIndex(lempelZivMatch.Displacement);
            var entry = GetDisplacementEntry(dispIndex);

            bw.WriteBits(dispIndex, 2);
            bw.WriteBits((int)lempelZivMatch.Displacement - entry.DisplacementStart, entry.ReadBits);
            bw.WriteBits((int)lempelZivMatch.Length - 3, 4);
        }
    }
}
