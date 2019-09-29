using System;
using System.Collections.Generic;

namespace Kompression.MatchFinders.Support
{
    // TODO: Documentation
    class SuffixTreeNode : IDisposable
    {
        public int Start { get; set; }
        public IntValue End { get; private set; }

        public Dictionary<byte, SuffixTreeNode> Children { get; private set; } = new Dictionary<byte, SuffixTreeNode>();

        public SuffixTreeNode(int start, IntValue end)
        {
            Start = start;
            End = end;
        }

        public int CalculateLength() => End.Value - Start + 1;

        public void Dispose()
        {
            End = null;
            Children = null;
        }
    }
}
