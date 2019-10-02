using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kompression.Configuration;
using Kompression.Interfaces;
using Kompression.MatchFinders.Parallel;
using Kompression.MatchFinders.Support;
using Kompression.Models;

namespace Kompression.MatchFinders
{
    /// <summary>
    /// Finding matches using a <see cref="HybridSuffixTree"/>.
    /// </summary>
    public class HybridSuffixTreeMatchFinder : IMatchFinder
    {
        private HybridSuffixTree _tree;

        /// <inheritdoc cref="FindLimitations"/>
        public FindLimitations FindLimitations { get; private set; }

        /// <inheritdoc cref="FindOptions"/>
        public FindOptions FindOptions { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="HybridSuffixTreeMatchFinder"/>.
        /// </summary>
        /// <param name="limits">The search limitations to find pattern matches.</param>
        /// <param name="options">The additional configuration for finding pattern matches.</param>
        public HybridSuffixTreeMatchFinder(FindLimitations limits, FindOptions options)
        {
            _tree = new HybridSuffixTree();

            FindLimitations = limits;
            FindOptions = options;
        }

        /// <inheritdoc cref="FindMatchesAtPosition"/>
        public IEnumerable<Match> FindMatchesAtPosition(byte[] input, int position)
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
                var previousLength = length;
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

        /// <inheritdoc cref="GetAllMatches"/>
        public IEnumerable<Match[]> GetAllMatches(byte[] input, int position)
        {
            if (!_tree.IsBuilt)
                _tree.Build(input, position);

            var taskCount = FindOptions.TaskCount;
            var unitSize = (int)FindOptions.UnitSize;

            var tasks = new Task<bool>[taskCount];
            var enumerators = new MatchFinderEnumerator[taskCount];

            // Setup enumerators
            for (var i = 0; i < taskCount; i++)
                enumerators[i] = new MatchFinderEnumerator(input, position, taskCount * unitSize, FindMatchesAtPosition);

            // Execute all tasks until end of file
            var continueExecution = true;
            while (continueExecution)
            {
                for (int i = 0; i < taskCount; i++)
                {
                    var enumerator = enumerators[i];
                    tasks[i] = new Task<bool>(() => enumerator.MoveNext());
                    tasks[i].Start();
                }

                Task.WaitAll(tasks);
                continueExecution = tasks.All(x => x.Result);

                for (var i = 0; i < taskCount; i++)
                    if (tasks[i].Result)
                        yield return enumerators[i].Current;
            }
        }

        #region Dispose

        public void Dispose()
        {
            _tree.Dispose();

            _tree = null;
            FindLimitations = null;
            FindOptions = null;
        }

        #endregion
    }
}
