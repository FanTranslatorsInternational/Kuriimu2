using System;
using System.Linq;
using Kompression.LempelZiv.MatchFinder.Models;

namespace Kompression.LempelZiv.MatchFinder
{
    // TODO: Implement using all offsets in tree
    public class HybridSuffixTreeMatchFinder : ILongestMatchFinder
    {
        private readonly HybridSuffixTree _tree;

        public int MinMatchSize { get; }
        public int MaxMatchSize { get; }
        public int WindowSize { get; }
        public int MinDisplacement { get; }

        public HybridSuffixTreeMatchFinder(int minMatchSize, int maxMatchSize, int windowSize, int minDisplacement)
        {
            _tree = new HybridSuffixTree();

            MinMatchSize = minMatchSize;
            MaxMatchSize = maxMatchSize;
            WindowSize = windowSize;
            MinDisplacement = minDisplacement;
        }

        /// <summary>
        /// Finds the longest and nearest match to the position in the file.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public LzMatch FindLongestMatch(byte[] input, int position)
        {
            if (!_tree.IsBuilt)
                _tree.Build(input, 0);

            var originalPosition = position;
            var length = 0;

            if (position >= input.Length || !_tree.Root.Children.ContainsKey(input[position]))
                return null;

            var node = _tree.Root.Children[input[position]];
            var offsets = _tree.GetOffsets(input[position], position, WindowSize, MinDisplacement);
            while (node != null && length < MaxMatchSize && offsets.Length > 0)
            {
                length += node.CalculateLength();
                position += node.CalculateLength();

                if (position < input.Length &&
                    node.Children.ContainsKey(input[position]) &&
                    node.Children[input[position]].Start < position)
                    node = node.Children[input[position]];
                else
                    node = null;

                if (node != null)
                    offsets = offsets.Where(x => input[x + length] == input[position]).ToArray();
            }

            if (length >= MinMatchSize && offsets.Length > 0)
                return new LzMatch(originalPosition, originalPosition - offsets[0], Math.Min(MaxMatchSize, length));

            return null;
        }

        // TODO: Rework FindAllMatches with HybridSuffixTree
        //public LzMatch[] FindAllMatches(Span<byte> input, int position)
        //{
        //    throw new NotImplementedException();

        //    if (!_tree.IsBuilt)
        //        _tree.Read(input.ToArray(), position);

        //    var results = new List<LzMatch>();

        //    var originalPosition = position;
        //    var displacement = 0;
        //    var length = 0;

        //    var node = _tree.Root;
        //    do
        //    {
        //        node = Traverse(node, input, ref position, ref displacement, ref length);
        //        if (displacement > 0 && length >= MinMatchSize && length < MaxMatchSize)
        //            results.Add(new LzMatch(originalPosition, displacement, length));
        //    } while (node != null && length < MaxMatchSize);

        //    return results.ToArray();
        //}

        //private SuffixTreeNode Traverse(SuffixTreeNode node, Span<byte> input, ref int position, ref int displacement, ref int length)
        //{
        //    if (node == null)
        //        return null; // no match possible

        //    //If node n is not root node, then traverse edge 
        //    //from node n's parent to node n. 
        //    if (node.Start != -1)
        //    {
        //        displacement = position - node.Start;
        //        var res = TraverseEdge(node, input, position, input.Length, ref length);
        //        if (res != 0)
        //            return null;  // matching stopped (res = -1) 

        //        //Get the character index to search 
        //        position += node.CalculateLength();
        //    }

        //    //If there is an edge from node n going out 
        //    //with current character str[idx], traverse that edge 
        //    if (position < input.Length)
        //        if (node.Children[input[position]] != null)
        //            if (node.Children[input[position]].Start < position)
        //                return node.Children[input[position]];

        //    return null;  // no more children
        //}

        //private int TraverseEdge(SuffixTreeNode node, Span<byte> input, int position, int size, ref int length)
        //{
        //    //Traverse the edge with character by character matching 
        //    for (var k = node.Start; k <= node.End && position < size; k++, position++)
        //    {
        //        if (input[k] != input[position])
        //            return -1;  // stopped matching

        //        length++;
        //        if (length >= MaxMatchSize)
        //            return -1;  // stopped matching
        //    }

        //    return 0;  // matched whole node
        //}

        #region Dispose

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
                _tree.Dispose();
        }

        #endregion
    }
}
