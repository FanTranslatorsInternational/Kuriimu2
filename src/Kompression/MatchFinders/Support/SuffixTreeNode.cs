using System;
using System.Collections.Generic;

namespace Kompression.MatchFinders.Support
{
    /// <summary>
    /// A node in the suffix tree.
    /// </summary>
    class SuffixTreeNode : IDisposable
    {
        /// <summary>
        /// Gets or sets the position in the input where this node begins.
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// Gets the position in the input where this node ends.
        /// </summary>
        public IntValue End { get; private set; }

        /// <summary>
        /// Gets the length for this node.
        /// </summary>
        public int Length => End.Value - Start + 1;

        /// <summary>
        /// Gets the underlying children for this node.
        /// </summary>
        public Dictionary<byte, SuffixTreeNode> Children { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="SuffixTreeNode"/>.
        /// </summary>
        /// <param name="start">The start position in the input.</param>
        /// <param name="end">The end position in the input.</param>
        public SuffixTreeNode(int start, IntValue end)
        {
            Start = start;
            End = end;
            Children = new Dictionary<byte, SuffixTreeNode>();
        }

        /// <inheritdoc cref="Dispose"/>
        public void Dispose()
        {
            End = null;
            Children = null;
        }
    }
}
