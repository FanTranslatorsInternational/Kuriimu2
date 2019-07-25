using System;
using System.Collections.Generic;
using Kompression.LempelZiv.MatchFinder.Models;

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

            if (displacement > 0 && length >= MinMatchSize)
                return new LzMatch(originalPosition, displacement, Math.Min(Math.Min(length, input.Length - originalPosition), MaxMatchSize));

            return null;
        }

        public LzMatch[] FindAllMatches(Span<byte> input, int position)
        {
            if (!_tree.IsBuilt)
                _tree.Build(input.ToArray(), position);

            var results = new List<LzMatch>();

            var originalPosition = position;
            var displacement = 0;
            var length = 0;

            var node = _tree.Root.Children[input[position]];
            var oldStart = node.Start;
            do
            {
                var addMatch = oldStart != node.Start - length;

                if (addMatch && displacement > 0 && length >= MinMatchSize && length < MaxMatchSize)
                    results.Add(new LzMatch(originalPosition, displacement, length));

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

            if (displacement > 0 && length >= MinMatchSize && length < MaxMatchSize)
                results.Add(new LzMatch(originalPosition, displacement, length));

            return results.ToArray();
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
