using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kompression.LempelZiv.Occurrence.Models
{
    [DebuggerDisplay("{Index}")]
    class SuffixTreeChild
    {
        public int Index { get; }
        public SuffixTreeNode Node { get; set; }

        public SuffixTreeChild(int index, SuffixTreeNode node)
        {
            Index = index;
            Node = node;
        }
    }
}
