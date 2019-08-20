using Kompression.IO;

namespace Kompression.Specialized.SlimeMoriMori.ValueWriters
{
    interface IValueWriter
    {
        void WriteValue(BitWriter bw, byte value);
    }
}
