using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Kompression.LempelZiv.Matcher.Models
{
    class SuffixTreeNode:IDisposable
    {
        private readonly IntPtr _end;

        public int Start { get; set; }
        public int End => Marshal.ReadInt32(_end);

        public Dictionary<byte, SuffixTreeNode> Children { get; private set; } = new Dictionary<byte, SuffixTreeNode>();

        public SuffixTreeNode(int start, IntPtr end)
        {
            Start = start;
            _end = end;
        }

        public int CalculateLength() => End - Start + 1;

        public void Dispose()
        {
            Dispose(true);
        }

        ~SuffixTreeNode()
        {
            Dispose(false);
        }

        private void Dispose(bool dispose)
        {
            if (!dispose)
            {
                Marshal.FreeHGlobal(_end);
            }
            else
            {
                Children = null;
            }
        }
    }
}
