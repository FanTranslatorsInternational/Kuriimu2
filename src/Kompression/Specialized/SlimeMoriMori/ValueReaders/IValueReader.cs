using System;
using Komponent.IO;

namespace Kompression.Specialized.SlimeMoriMori.ValueReaders
{
    interface IValueReader : IDisposable
    {
        void BuildTree(BitReader br);

        byte ReadValue(BitReader br);
    }
}
