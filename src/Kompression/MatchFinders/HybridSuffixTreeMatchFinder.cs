using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kompression.Configuration;
using Kompression.Interfaces;
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
                yield return new Match(originalPosition, originalPosition - offsets[0], length);
        }

        public IEnumerable<Match> GetAllMatches(byte[] input, int position)
        {
            if (!_tree.IsBuilt)
                _tree.Build(input, position);

            var taskCount = 8;
            var unitSize = 1;
            var tasks = new Task<IList<Match>>[taskCount];

            for (int i = 0; i < taskCount; i += unitSize)
            {
                var getTaskPosition = position + i;
                tasks[i] = new Task<IList<Match>>(() => GetMatchFromTask(input, getTaskPosition, taskCount).ToList());
                tasks[i].Start();
            }

            Task.WaitAll(tasks);

            return tasks.SelectMany(x => x.Result).OrderBy(x => x.Position);
        }

        private IEnumerable<Match> GetMatchFromTask(byte[] input, int startPosition, int interval)
        {
            for (var i = startPosition; i < input.Length; i += interval)
            {
                foreach (var match in FindMatchesAtPosition(input, i))
                    yield return match;
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
