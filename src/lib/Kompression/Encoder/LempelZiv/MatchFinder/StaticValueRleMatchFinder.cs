using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.DataClasses.Encoder.LempelZiv.MatchFinder;
using Kompression.Contract.Encoder.LempelZiv.MatchFinder;
using Kompression.Contract.Enums.Encoder.LempelZiv;

namespace Kompression.Encoder.LempelZiv.MatchFinder
{
    internal class StaticValueRleMatchFinder(int value, LempelZivMatchFinderOptions options) : ILempelZivMatchFinder
    {
        /// <inheritdoc />
        public LempelZivMatchFinderOptions Options { get; } = options;

        public void PreProcess(byte[] input)
        {
        }

        public LempelZivAggregateMatch? FindMatchesAtPosition(byte[] input, int position)
        {
            if (input.Length - position < Options.Limitations.MinLength)
                return null;

            int maxLength = Options.Limitations.MaxLength <= 0 ? input.Length : Options.Limitations.MaxLength;
            var unitSize = (int)Options.UnitSize;

            int cappedLength = Math.Min(maxLength, input.Length - unitSize - position);
            for (var repetitions = 0; repetitions < cappedLength; repetitions += unitSize)
            {
                switch (Options.UnitSize)
                {
                    case UnitSize.Byte:
                        if (input[position + repetitions] != value)
                        {
                            if (repetitions > 0 && repetitions >= Options.Limitations.MinLength)
                                return new LempelZivAggregateMatch(0, repetitions);

                            return null;
                        }
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported unit size {Options.UnitSize}.");
                }
            }

            return new LempelZivAggregateMatch(0, cappedLength - cappedLength % (int)Options.UnitSize);
        }
    }
}
