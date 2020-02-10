using System;
using Kontract.Kompression;
using Kontract.Kompression.Model;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.PatternMatch.MatchFinders
{
    /// <summary>
    /// Find sequences of the same value.
    /// </summary>
    public class RleMatchFinder : IMatchFinder
    {
        /// <inheritdoc />
        public FindLimitations FindLimitations { get; }

        /// <inheritdoc />
        public FindOptions FindOptions { get; }

        /// <summary>
        /// Creates a new instance of <see cref="RleMatchFinder"/>.
        /// </summary>
        /// <param name="limits">The limits to search sequences in.</param>
        /// <param name="options">The options to search sequences with.</param>
        public RleMatchFinder(FindLimitations limits, FindOptions options)
        {
            FindLimitations = limits;
            FindOptions = options;
        }

        /// <inheritdoc />
        public void PreProcess(byte[] input, int startPosition)
        {
        }

        /// <inheritdoc />
        public AggregateMatch FindMatchesAtPosition(byte[] input, int position)
        {
            if (input.Length - position < FindLimitations.MinLength)
                return null;

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
                            if (repetitions > 0 && repetitions >= FindLimitations.MinLength)
                                return new AggregateMatch(0, repetitions);
                        }
                        break;

                    default:
                        throw new InvalidOperationException($"UnitSize '{FindOptions.UnitSize}' is not supported.");
                }
            }

            return new AggregateMatch(0, cappedLength - cappedLength % (int)FindOptions.UnitSize);
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
