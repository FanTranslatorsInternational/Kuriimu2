using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kompression.Specialized.SlimeMoriMori.Decoders
{
    class DisplacementElement
    {
        public byte ReadBits { get; }
        public short DisplacementStart { get; }

        public DisplacementElement(byte readBits, short dispalcementStart)
        {
            ReadBits = readBits;
            DisplacementStart = dispalcementStart;
        }
    }
}
