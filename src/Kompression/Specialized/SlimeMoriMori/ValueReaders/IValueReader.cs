using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.IO;

namespace Kompression.Specialized.SlimeMoriMori.ValueReaders
{
    interface IValueReader : IDisposable
    {
        void BuildTree(BitReader br);

        byte ReadValue(BitReader br);
    }
}
