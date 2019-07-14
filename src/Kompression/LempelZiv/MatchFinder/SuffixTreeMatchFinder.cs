using System;
using System.Collections.Generic;
using System.Linq;
using Kompression.LempelZiv.MatchFinder.Models;
using Kompression.LempelZiv.Models;

namespace Kompression.LempelZiv.MatchFinder
{
    public class SuffixTreeMatchFinder : ILzMatchFinder
    {
        private readonly SuffixTree _tree;

        public int MinMatchSize { get; }
        public int MaxMatchSize { get; }

        public SuffixTreeMatchFinder(int minMatchSize, int maxMatchSize)
        {
            _tree = new SuffixTree();

            MinMatchSize = minMatchSize;
            MaxMatchSize = maxMatchSize;
        }

        /// <summary>
        /// Finds the longest and nearest match to the position in the file.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public LzMatch FindLongestMatch(Span<byte> input, int position)
        {
            if (!_tree.IsBuilt)
                _tree.Build(input, 0);

            var originalPosition = position;
            var longestLength = 0;
            LzMatch longestMatch = null;

            if (position >= input.Length || !_tree.Root.Children.ContainsKey(input[position]))
                return null;

            var startNode = _tree.Root.Children[input[position]];
            foreach (var offset in _tree.GetOffsets(input[position]).Where(x => x < position).OrderByDescending(x => x).Take(5))
            {
                position = originalPosition;
                var node = startNode;
                var length = 0;
                var displacement = position - offset;

                do
                {
                    length += node.CalculateLength();
                    position += node.CalculateLength();

                    if (position < input.Length &&
                        input[offset + length] == input[position] &&
                        node.Children.ContainsKey(input[position]) &&
                        node.Children[input[position]].Start < position)
                        node = node.Children[input[position]];
                    else
                        node = null;
                } while (node != null && length < MaxMatchSize && position < input.Length);

                if (length > longestLength && length > MinMatchSize)
                {
                    longestLength = length;
                    longestMatch = new LzMatch(originalPosition, displacement, Math.Min(Math.Min(length, input.Length - originalPosition), MaxMatchSize));
                }
            }

            return longestMatch;
        }

        //public LzMatch[] FindAllMatches(Span<byte> input, int position)
        //{
        //    if (!_tree.IsBuilt)
        //        _tree.Build(input, 0);

        //    var originalPosition = position;
        //    int displacement;
        //    var length = 0;

        //    if (position >= input.Length || !_tree.Root.Children.ContainsKey(input[position]))
        //        return null;
        //}

        private SuffixTreeNode Traverse2(SuffixTreeNode node, Span<byte> input, ref int position, ref int displacement, ref int length)
        {
            if (node == null)
                return null; // no match possible

            //If node n is not root node, then traverse edge 
            //from node n's parent to node n. 
            if (node.Start != -1)
            {
                displacement = position - node.Start;
                length += node.CalculateLength();
                //var res = TraverseEdge(node, input, position, input.Length, ref length);
                //if (res != 0)
                //    return null;  // matching stopped (res = -1) 

                //Get the character index to search 
                position += node.CalculateLength();
            }

            //If there is an edge from node n going out 
            //with current character str[idx], traverse that edge 
            if (position < input.Length &&
                node.Children.ContainsKey(input[position]) &&
                node.Children[input[position]].Start < position)
                return node.Children[input[position]];

            return null;  // no more children
        }

        public LzMatch FindLongestMatch2(Span<byte> input, int position)
        {
            if (!_tree.IsBuilt)
                _tree.Build(input.ToArray(), position);

            var originalPosition = position;
            var displacement = 0;
            var length = 0;

            var node = _tree.Root;
            do
            {
                node = Traverse(node, input, ref position, ref displacement, ref length);
            } while (node != null && length < MaxMatchSize);

            return displacement > 0 && length >= MinMatchSize ? new LzMatch(originalPosition, displacement, length) : null;
        }

        public LzMatch[] FindAllMatches(Span<byte> input, int position)
        {
            if (!_tree.IsBuilt)
                _tree.Build(input.ToArray(), position);

            var results = new List<LzMatch>();

            var originalPosition = position;
            var displacement = 0;
            var length = 0;

            var node = _tree.Root;
            do
            {
                node = Traverse(node, input, ref position, ref displacement, ref length);
                if (displacement > 0 && length >= MinMatchSize && length < MaxMatchSize)
                    results.Add(new LzMatch(originalPosition, displacement, length));
            } while (node != null && length < MaxMatchSize);

            return results.ToArray();
        }

        private SuffixTreeNode Traverse(SuffixTreeNode node, Span<byte> input, ref int position, ref int displacement, ref int length)
        {
            if (node == null)
                return null; // no match possible

            //If node n is not root node, then traverse edge 
            //from node n's parent to node n. 
            if (node.Start != -1)
            {
                displacement = position - node.Start;
                var res = TraverseEdge(node, input, position, input.Length, ref length);
                if (res != 0)
                    return null;  // matching stopped (res = -1) 

                //Get the character index to search 
                position += node.CalculateLength();
            }

            //If there is an edge from node n going out 
            //with current character str[idx], traverse that edge 
            if (position < input.Length)
                if (node.Children[input[position]] != null)
                    if (node.Children[input[position]].Start < position)
                        return node.Children[input[position]];

            return null;  // no more children
        }

        private int TraverseEdge(SuffixTreeNode node, Span<byte> input, int position, int size, ref int length)
        {
            //Traverse the edge with character by character matching 
            for (var k = node.Start; k <= node.End && position < size; k++, position++)
            {
                if (input[k] != input[position])
                    return -1;  // stopped matching

                length++;
                if (length >= MaxMatchSize)
                    return -1;  // stopped matching
            }

            return 0;  // matched whole node
        }

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
