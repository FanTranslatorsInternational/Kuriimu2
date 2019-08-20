using System;
using System.Collections.Generic;
using System.Linq;
using Kompression.LempelZiv.MatchFinder.Models;

namespace Kompression.LempelZiv.MatchFinder
{
    public class SuffixTreeMatchFinder : ILongestMatchFinder, IAllMatchFinder
    {
        private readonly SuffixTree _tree;

        public int MinMatchSize { get; }
        public int MaxMatchSize { get; }
        public int MinDisplacement { get; }
        public int UnitLength { get; }

        public SuffixTreeMatchFinder(int minMatchSize, int maxMatchSize, int minDisplacement)
        {
            _tree = new SuffixTree();

            // TODO: Support other unit lengths
            UnitLength = 1;
            MinMatchSize = minMatchSize;
            MaxMatchSize = maxMatchSize;
            MinDisplacement = minDisplacement;
        }

        /// <summary>
        /// Finds the longest match to the position in the file.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public LzMatch FindLongestMatch(byte[] input, int position)
        {
            if (!_tree.IsBuilt)
                _tree.Build(input, 0);

            var originalPosition = position;
            int displacement;
            var length = 0;

            var node = _tree.Root.Children[input[position]];
            do
            {
                displacement = position - node.Start;
                length += node.CalculateLength();
                position += node.CalculateLength();

                if (position < input.Length &&
                    node.Children.ContainsKey(input[position]) &&
                    node.Children[input[position]].Start < position)
                    node = node.Children[input[position]];
                else
                    node = null;
            } while (node != null && length < MaxMatchSize);

            if (displacement >= MinDisplacement && length >= MinMatchSize)
                return new LzMatch(originalPosition, displacement, Math.Min(Math.Min(length, input.Length - originalPosition), MaxMatchSize));

            return null;
        }

        public IEnumerable<LzMatch> FindAllMatches(byte[] input, int position, int limit = -1)
        {
            var searchResults = FindAllMatchesInternal(input, position);
            if (limit >= 0)
                searchResults = searchResults.Take(limit);
            return searchResults;
        }

        private IEnumerable<LzMatch> FindAllMatchesInternal(byte[] input, int position)
        {
            if (!_tree.IsBuilt)
                _tree.Build(input, position);

            var originalPosition = position;
            var displacement = 0;
            var length = 0;

            var node = _tree.Root.Children[input[position]];
            var oldStart = node.Start;
            do
            {
                var addMatch = oldStart != node.Start - length;

                if (addMatch && displacement >= MinDisplacement && length >= MinMatchSize && length < MaxMatchSize)
                    yield return new LzMatch(originalPosition, displacement, length);

                displacement = position - node.Start;
                length += node.CalculateLength();
                position += node.CalculateLength();
                oldStart = node.Start;

                if (position < input.Length &&
                    node.Children.ContainsKey(input[position]) &&
                    node.Children[input[position]].Start < position)
                    node = node.Children[input[position]];
                else
                    node = null;
            } while (node != null && length < MaxMatchSize);

            if (displacement >= MinDisplacement && length >= MinMatchSize && length < MaxMatchSize)
                yield return new LzMatch(originalPosition, displacement, length);
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
