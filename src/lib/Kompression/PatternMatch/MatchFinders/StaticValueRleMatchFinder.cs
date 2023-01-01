using System;
using Kontract.Kompression.Interfaces;
using Kontract.Kompression.Models;
using Kontract.Kompression.Models.PatternMatch;

namespace Kompression.PatternMatch.MatchFinders
{
    class StaticValueRleMatchFinder : IMatchFinder
    {
        private readonly int _valueToMatch;

        public FindLimitations FindLimitations { get; }
        public FindOptions FindOptions { get; }

        public StaticValueRleMatchFinder(int valueToMatch, FindLimitations limits, FindOptions options)
        {
            _valueToMatch = valueToMatch;

            FindLimitations = limits;
            FindOptions = options;
        }

        public void PreProcess(byte[] input)
        {
        }

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
                        if (input[position + repetitions] != _valueToMatch)
                        {
                            if (repetitions > 0 && repetitions >= FindLimitations.MinLength)
                                return new AggregateMatch(0, repetitions);

                            return null;
                        }
                        break;

                    default:
                        throw new InvalidOperationException($"UnitSize '{FindOptions.UnitSize}' is not supported.");
                }
            }

            return new AggregateMatch(0, cappedLength - cappedLength % (int)FindOptions.UnitSize);
        }

        public void Dispose()
        {
        }
    }
}
