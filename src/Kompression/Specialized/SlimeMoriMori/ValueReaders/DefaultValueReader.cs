using Komponent.IO;
using Kompression.IO;

namespace Kompression.Specialized.SlimeMoriMori.ValueReaders
{
    class DefaultValueReader : IValueReader
    {
        public void BuildTree(BitReader br)
        {
            // We don't have a tree here
        }

        public byte ReadValue(BitReader br)
        {
            return br.ReadBits<byte>(8);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
