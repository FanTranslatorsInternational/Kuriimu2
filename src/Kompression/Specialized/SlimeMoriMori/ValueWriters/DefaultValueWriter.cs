using Kompression.IO;

namespace Kompression.Specialized.SlimeMoriMori.ValueWriters
{
    class DefaultValueWriter : IValueWriter
    {
        public void WriteValue(BitWriter bw, byte value)
        {
            bw.WriteBits(value, 8);
        }
    }
}
