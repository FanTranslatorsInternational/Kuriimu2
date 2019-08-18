using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.IO;

namespace Kompression.Specialized.SlimeMoriMori.ValueWriters
{
    interface IValueWriter
    {
        void WriteValue(BitWriter bw, byte value);
    }
}
