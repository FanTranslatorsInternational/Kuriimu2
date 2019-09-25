using System;
using System.Collections.Generic;
using System.Linq;
using Kompression.PatternMatch.LempelZiv.Support;

namespace Kompression.PatternMatch.LempelZiv
{
    /// <summary>
    /// Finding matches using <see cref="HybridSuffixTree"/>.
    /// </summary>
    public class HybridSuffixTreeMatchFinder : IMatchFinder
    {
        private readonly HybridSuffixTree _tree;

        public int MinMatchSize { get; }
        public int MaxMatchSize { get; }
        public int MinDisplacement { get; }
        public int MaxDisplacement { get; }
        public DataType DataType { get; }
        public bool UseLookAhead { get; }

        public HybridSuffixTreeMatchFinder(int minMatchSize, int maxMatchSize, int minDisplacement, int maxDisplacement,
            bool lookAhead = true, DataType dataType = DataType.Byte)
        {
            _tree = new HybridSuffixTree();

            if (minMatchSize % (int)dataType != 0 || maxMatchSize % (int)dataType != 0 ||
               minDisplacement % (int)dataType != 0 || maxDisplacement % (int)dataType != 0)
                throw new InvalidOperationException("All values must be dividable by data type.");

            MinMatchSize = minMatchSize;
            MaxMatchSize = maxMatchSize;
            MinDisplacement = minDisplacement;
            MaxDisplacement = maxDisplacement;
            DataType = dataType;
            UseLookAhead = lookAhead;
        }

        /// <summary>
        /// Finds the longest and nearest match to the position in the file.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public IEnumerable<Match> FindMatches(byte[] input, int position)
        {
            if (!_tree.IsBuilt)
                _tree.Build(input, 0);

            var originalPosition = position;
            var length = 0;

            if (position >= input.Length || !_tree.Root.Children.ContainsKey(input[position]))
                yield break;

            var lookAheadCapOffsets = new List<(int offset, int length)>();

            var node = _tree.Root.Children[input[position]];
            var offsets = _tree.GetOffsets(input[position], position, MaxDisplacement, MinDisplacement, DataType);
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
                {
                    if (!UseLookAhead)
                    {
                        var cappedOffsets = offsets.Where(x => x + length >= originalPosition);
                        lookAheadCapOffsets.AddRange(cappedOffsets.Select(x => (x, originalPosition - x)));
                        offsets = offsets.Where(x => x + length < originalPosition).ToArray();
                    }
                    offsets = offsets.Where(x => input[x + length] == input[position]).ToArray();
                }
            }

            length = Math.Min(MaxMatchSize, length);
            if (length % (int)DataType != 0)
                length -= length % (int)DataType;

            if (length >= MinMatchSize)
                if (offsets.Length > 0)
                    yield return new Match(originalPosition, originalPosition - offsets[0], length);
                else
                {
                    var lookAheadCapOffset = lookAheadCapOffsets.OrderByDescending(x => x.length)
                        .ThenByDescending(x => x.offset).FirstOrDefault();
                    if (lookAheadCapOffset != default)
                        yield return new Match(originalPosition, originalPosition - lookAheadCapOffset.offset, lookAheadCapOffset.length);
                }
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
