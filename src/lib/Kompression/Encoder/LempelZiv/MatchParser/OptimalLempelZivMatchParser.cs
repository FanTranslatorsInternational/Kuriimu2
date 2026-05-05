using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.DataClasses.Encoder.LempelZiv.MatchParser;
using Kompression.Contract.Encoder.LempelZiv.MatchFinder;
using Kompression.DataClasses.Encoder.LemeplZiv.MatchParser;

namespace Kompression.Encoder.LempelZiv.MatchParser
{
    public class OptimalLempelZivMatchParser : LempelZivMatchParser
    {
        public OptimalLempelZivMatchParser(LempelZivMatchParserOptions options) : base(options)
        {
            if (Options.MatchFinders.Any(x => x.Options.UnitSize != Options.MatchFinders[0].Options.UnitSize))
                throw new InvalidOperationException("All Match finder have to have the same unit size.");
        }

        protected override IEnumerable<LempelZivMatch> InternalParseMatches(byte[] input, int startPosition)
        {
            // Initialize history
            var history = new MatchParserPositionData[input.Length - startPosition + 1];
            for (var i = 0; i < history.Length; i++)
                history[i] = new MatchParserPositionData(0, false, null, int.MaxValue);
            history[0].Price = 0;

            // Two-Pass for optimal parsing
            ForwardPass(input, startPosition, history);
            return BackwardPass(history).Reverse();
        }

        private void ForwardPass(byte[] input, int startPosition, MatchParserPositionData[] history)
        {
            var matches = GetAllMatches(input, startPosition);

            var unitSize = (int)Options.UnitSize;
            for (var dataPosition = 0; dataPosition < input.Length - startPosition; dataPosition += unitSize)
            {
                // Calculate literal place at position
                var element = history[dataPosition];
                var newRunLength = element.IsMatchRun ? unitSize : element.CurrentRunLength + unitSize;
                var isFirstLiteralRun = IsFirstLiteralRun(dataPosition, unitSize, history);
                var literalPrice = Options.PriceCalculator.CalculateLiteralPrice(input[dataPosition], newRunLength, isFirstLiteralRun);
                literalPrice += element.Price;

                if (dataPosition + unitSize < history.Length &&
                    literalPrice <= history[dataPosition + unitSize].Price)
                {
                    var nextElement = history[dataPosition + unitSize];

                    nextElement.Parent = element;
                    nextElement.Price = literalPrice;
                    nextElement.CurrentRunLength = newRunLength;
                    nextElement.IsMatchRun = false;
                    nextElement.Match = null;
                }

                // Then go through all longest matches at current position
                for (var finderIndex = 0; finderIndex < Options.MatchFinders.Length; finderIndex++)
                {
                    var finderMatch = matches[finderIndex][dataPosition];
                    if (finderMatch == null || !finderMatch.HasMatches)
                        continue;

                    for (var j = Options.MatchFinders[finderIndex].Options.Limitations.MinLength; j <= finderMatch.MaxLength; j += unitSize)
                    {
                        var displacement = finderMatch.GetDisplacement(j);
                        if (displacement < 0)
                            continue;

                        newRunLength = element.IsMatchRun ? element.CurrentRunLength + 1 : 1;
                        var matchPrice = Options.PriceCalculator.CalculateMatchPrice(displacement, j, newRunLength, input[dataPosition]);
                        matchPrice += element.Price;

                        if (dataPosition + j < history.Length &&
                            matchPrice < history[dataPosition + j].Price)
                        {
                            var nextElement = history[dataPosition + j];

                            nextElement.Parent = element;
                            nextElement.Price = matchPrice;
                            nextElement.CurrentRunLength = newRunLength;
                            nextElement.IsMatchRun = true;
                            nextElement.Match = new LempelZivMatch(dataPosition, displacement, j);
                        }
                    }
                }
            }
        }

        private IEnumerable<LempelZivMatch> BackwardPass(MatchParserPositionData[] history)
        {
            var element = history.Last();
            var position = history.Length - 1;
            while (element != null)
            {
                if (element.Match != null)
                {
                    position -= element.Match.Length;
                    yield return new LempelZivMatch(position, element.Match.Displacement, element.Match.Length);
                }
                else
                {
                    position -= (int)Options.UnitSize;
                }

                element = element.Parent;
            }
        }

        private IList<LempelZivAggregateMatch?>[] GetAllMatches(byte[] input, int startPosition)
        {
            var result = new IList<LempelZivAggregateMatch?>[Options.MatchFinders.Length];

            for (var i = 0; i < Options.MatchFinders.Length; i++)
            {
                ILempelZivMatchFinder finder = Options.MatchFinders[i];

                result[i] = Enumerable.Range(startPosition, input.Length)
                    .AsParallel()
                    .AsOrdered()
                    .WithDegreeOfParallelism(Options.TaskCount)
                    .Select(x => finder.FindMatchesAtPosition(input, x)).ToArray();
            }

            return result;
        }

        private static bool IsFirstLiteralRun(int dataPosition, int unitSize, MatchParserPositionData[] history)
        {
            while (dataPosition >= 0)
            {
                if (history[dataPosition].Match != null)
                    return false;

                dataPosition -= unitSize;
            }

            return true;
        }
    }
}
