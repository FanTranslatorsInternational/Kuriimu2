using Komponent.IO;
using Kompression.InternalContract.SlimeMoriMori.ValueWriter;

namespace Kompression.Specialized.SlimeMoriMori.ValueWriter
{
    internal class DefaultValueWriter : IValueWriter
    {
        public void WriteValue(BinaryBitWriter bw, byte value)
        {
            bw.WriteBits(value, 8);
        }
    }
}
