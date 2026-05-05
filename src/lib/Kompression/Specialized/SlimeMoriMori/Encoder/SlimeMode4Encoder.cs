using Komponent.IO;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.InternalContract.SlimeMoriMori.Encoder;
using Kompression.InternalContract.SlimeMoriMori.ValueWriter;

namespace Kompression.Specialized.SlimeMoriMori.Encoder
{
    internal class SlimeMode4Encoder(IValueWriter valueWriter) : ISlimeEncoder
    {
        public void Encode(Stream input, BinaryBitWriter bw, LempelZivMatch[] matches)
        {
            while (input.Position < input.Length)
            {
                valueWriter.WriteValue(bw, (byte)input.ReadByte());
            }
        }
    }
}
