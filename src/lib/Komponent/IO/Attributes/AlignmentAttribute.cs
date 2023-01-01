using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komponent.IO.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class AlignmentAttribute : Attribute
    {
        public int Alignment { get; }

        public AlignmentAttribute(int align)
        {
            Alignment = align;
        }
    }
}
