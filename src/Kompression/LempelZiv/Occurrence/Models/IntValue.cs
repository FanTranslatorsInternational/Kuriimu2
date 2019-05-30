using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kompression.LempelZiv.Occurrence.Models
{
    [DebuggerDisplay("{Value}")]
    class IntValue
    {
        public int Value { get; set; }

        public IntValue(int value)
        {
            Value = value;
        }
    }
}
