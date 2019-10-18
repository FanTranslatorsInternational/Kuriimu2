using System;
using System.Collections.Generic;
using System.Linq;
using Kompression.Configuration;
using Kompression.Models;
using Kompression.PatternMatch.MatchFinders.Parallel;
using Kompression.PatternMatch.MatchFinders.Support;

namespace Kompression.PatternMatch.MatchFinders
{
    /// <summary>
    /// Finding matches using a <see cref="HybridSuffixTree"/>.
    /// </summary>
    public class HybridSuffixTreeMatchFinder : BaseMatchFinder
    {
        private HybridSuffixTree _tree;

        /// <summary>
        /// Creates a new instance of <see cref="HybridSuffixTreeMatchFinder"/>.
        /// </summary>
        /// <param name="limits">The search limitations to find pattern matches.</param>
        /// <param name="options">The additional configuration for finding pattern matches.</param>
        public HybridSuffixTreeMatchFinder(FindLimitations limits, FindOptions options) :
            base(limits, options)
        {
            _tree = new HybridSuffixTree();
        }

        /// <inheritdoc cref="FindMatchesAtPosition(byte[],int)"/>
        public override IEnumerable<Match> FindMatchesAtPosition(byte[] input, int position)
        {
            if (!_tree.IsBuilt)
                _tree.Build(input, 0);

            var maxSize = FindLimitations.MaxLength <= 0 ? input.Length : FindLimitations.MaxLength;
            var maxDisplacement = FindLimitations.MaxDisplacement <= 0 ? input.Length : FindLimitations.MaxDisplacement;

            var originalPosition = position;
            var length = 0;

            if (position >= input.Length || !_tree.Root.Children.ContainsKey(input[position]))
                yield break;

            var node = _tree.Root.Children[input[position]];
            var offsets = _tree.GetOffsets(input[position], position, maxDisplacement, FindLimitations.MinDisplacement, (int)FindOptions.UnitSize);
            while (node != null && length < maxSize && offsets.Length > 0)
            {
                //var previousLength = length;
                length += node.Length;
                position += node.Length;

                if (position < input.Length &&
                    node.Children.ContainsKey(input[position]) &&
                    node.Children[input[position]].Start < position)
                    node = node.Children[input[position]];
                else
                    node = null;

                if (node != null)
                {
                    //if (previousLength >= FindLimitations.MinLength)
                    //{
                    //    var removedOffsets = offsets.Where(x => input[x + length] != input[position]);
                    //    foreach (var removedOffset in removedOffsets)
                    //        yield return new Match(originalPosition, originalPosition - removedOffset, previousLength);
                    //}
                    offsets = offsets.Where(x => input[x + length] == input[position]).ToArray();
                }
            }

            length = Math.Min(maxSize, length);
            length -= length % (int)FindOptions.UnitSize;

            if (length >= FindLimitations.MinLength)
                if (offsets.Length > 0)
                    yield return new Match(originalPosition, originalPosition - offsets[0], length);
        }

        /// <inheritdoc cref="SetupMatchFinder(byte[],int)"/>
        protected override void SetupMatchFinder(byte[] input, int startPosition)
        {
            if (!_tree.IsBuilt)
                _tree.Build(input, startPosition);
        }

        /// <inheritdoc cref="SetupMatchFinderEnumerators(byte[],int)"/>
        protected override MatchFinderEnumerator[] SetupMatchFinderEnumerators(byte[] input, int startPosition)
        {
            var taskCount = FindOptions.TaskCount;
            var unitSize = (int)FindOptions.UnitSize;

            var enumerators = new MatchFinderEnumerator[taskCount];
            for (var i = 0; i < taskCount; i++)
                enumerators[i] = new MatchFinderEnumerator(input, startPosition + unitSize * i, taskCount * unitSize, FindMatchesAtPosition);

            return enumerators;
        }

        /// <inheritdoc cref="Reset"/>
        public override void Reset()
        {
            // Nothing to reset
            _tree?.Dispose();
            _tree=new HybridSuffixTree();
        }

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tree?.Dispose();
                _tree = null;
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
