using System;
using System.Collections.Generic;
using Kompression.Configuration;
using Kompression.Models;
using Kompression.PatternMatch.MatchFinders.Parallel;

namespace Kompression.PatternMatch.MatchFinders
{
    /// <summary>
    /// Find sequences of the same value.
    /// </summary>
    public class RleMatchFinder : BaseMatchFinder
    {
        /// <summary>
        /// Creates a new instance of <see cref="RleMatchFinder"/>.
        /// </summary>
        /// <param name="limits">The limits to search sequences in.</param>
        /// <param name="options">The options to search sequences with.</param>
        public RleMatchFinder(FindLimitations limits, FindOptions options) :
            base(limits, options)
        {
        }

        /// <inheritdoc cref="FindMatchesAtPosition"/>
        public override IEnumerable<Match> FindMatchesAtPosition(byte[] input, int position)
        {
            if (input.Length - position < FindLimitations.MinLength)
                yield break;

            var maxLength = FindLimitations.MaxLength <= 0 ? input.Length : FindLimitations.MaxLength;
            var unitSize = (int)FindOptions.UnitSize;

            var cappedLength = Math.Min(maxLength, input.Length - unitSize - position);
            for (var repetitions = 0; repetitions < cappedLength; repetitions += unitSize)
            {
                switch (FindOptions.UnitSize)
                {
                    case UnitSize.Byte:
                        if (input[position + 1 + repetitions] != input[position])
                        {
                            if (repetitions > 0 && repetitions>=FindLimitations.MinLength)
                                yield return new Match(position, 0, repetitions);
                            yield break;
                        }
                        break;
                    default:
                        throw new InvalidOperationException($"UnitSize '{FindOptions.UnitSize}' is not supported.");
                }
            }

            yield return new Match(position, 0, cappedLength - cappedLength % (int)FindOptions.UnitSize);
        }

        /// <inheritdoc cref="Reset"/>
        public override void Reset()
        {
            // Nothing to reset
        }

        /// <inheritdoc cref="SetupMatchFinder"/>
        protected override void SetupMatchFinder(byte[] input, int startPosition)
        {
            // Nothing to setup
        }

        /// <inheritdoc cref="SetupMatchFinderEnumerators"/>
        protected override MatchFinderEnumerator[] SetupMatchFinderEnumerators(byte[] input, int startPosition)
        {
            var taskCount = FindOptions.TaskCount;
            var unitSize = (int)FindOptions.UnitSize;

            var enumerators = new MatchFinderEnumerator[taskCount];
            for (var i = 0; i < taskCount; i++)
                enumerators[i] = new MatchFinderEnumerator(input, startPosition + unitSize * i, taskCount * unitSize, FindMatchesAtPosition);

            return enumerators;
        }
    }
}
