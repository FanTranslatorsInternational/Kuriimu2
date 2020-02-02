using Komponent.IO;

namespace Kompression.Specialized.SlimeMoriMori.ValueWriters
{
    interface IValueWriter
    {
        void WriteValue(BitWriter bw, byte value);
    }
}
