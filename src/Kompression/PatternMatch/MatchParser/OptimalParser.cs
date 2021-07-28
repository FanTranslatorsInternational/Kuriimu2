using System;
using System.Collections.Generic;
using System.Linq;
using Kompression.PatternMatch.MatchParser.Support;
using Kontract.Kompression;
using Kontract.Kompression.Model;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.PatternMatch.MatchParser
{
    public class OptimalParser : BaseParser
    {
        private readonly IPriceCalculator _priceCalculator;

        public OptimalParser(FindOptions options, IPriceCalculator priceCalculator, params IMatchFinder[] finders):
            base(options,finders)
        {
            if (finders.Any(x => x.FindOptions.UnitSize != finders[0].FindOptions.UnitSize))
                throw new InvalidOperationException("All Match finder have to have the same unit size.");

            _priceCalculator = priceCalculator ?? throw new ArgumentNullException(nameof(priceCalculator));
        }

        protected override IEnumerable<Match> InternalParseMatches(byte[] input, int startPosition)
        {
            // Initialize history
            var history = new PositionElement[input.Length - startPosition + 1];
            for (var i = 0; i < history.Length; i++)
                history[i] = new PositionElement(0, false, null, int.MaxValue);
            history[0].Price = 0;

            // Two-Pass for optimal pathing
            ForwardPass(input, startPosition, history);
            return BackwardPass(history).Reverse();
        }

        private void ForwardPass(byte[] input, int startPosition, PositionElement[] history)
        {
            var matches = GetAllMatches(input, startPosition);

            var unitSize = (int)FindOptions.UnitSize;
            for (var dataPosition = 0; dataPosition < input.Length - startPosition; dataPosition += unitSize)
            {
                // Calculate literal place at position
                var element = history[dataPosition];
                var newRunLength = element.IsMatchRun ? unitSize : element.CurrentRunLength + unitSize;
                var isFirstLiteralRun = IsFirstLiteralRun(dataPosition, unitSize, history);
                var literalPrice = _priceCalculator.CalculateLiteralPrice(input[dataPosition], newRunLength, isFirstLiteralRun);
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
                for (var finderIndex = 0; finderIndex < Finders.Length; finderIndex++)
                {
                    var finderMatch = matches[finderIndex][dataPosition];
                    if (finderMatch == null || !finderMatch.HasMatches)
                        continue;

                    for (var j = Finders[finderIndex].FindLimitations.MinLength; j <= finderMatch.MaxLength; j += unitSize)
                    {
                        var displacement = finderMatch.GetDisplacement(j);
                        if (displacement < 0)
                            continue;

                        newRunLength = element.IsMatchRun ? element.CurrentRunLength + 1 : 1;
                        var matchPrice = _priceCalculator.CalculateMatchPrice(displacement, j, newRunLength, input[dataPosition]);
                        matchPrice += element.Price;

                        if (dataPosition + j < history.Length &&
                            matchPrice < history[dataPosition + j].Price)
                        {
                            var nextElement = history[dataPosition + j];

                            nextElement.Parent = element;
                            nextElement.Price = matchPrice;
                            nextElement.CurrentRunLength = newRunLength;
                            nextElement.IsMatchRun = true;
                            nextElement.Match = new Match(dataPosition, displacement, j);
                        }
                    }
                }
            }
        }

        private IEnumerable<Match> BackwardPass(PositionElement[] history)
        {
            var element = history.Last();
            var position = history.Length - 1;
            while (element != null)
            {
                if (element.Match != null)
                {
                    position -= element.Match.Length;
                    yield return new Match(position, element.Match.Displacement, element.Match.Length);
                }
                else
                {
                    position -= (int)FindOptions.UnitSize;
                }

                element = element.Parent;
            }
        }

        private IList<IList<AggregateMatch>> GetAllMatches(byte[] input, int startPosition)
        {
            var result = new IList<AggregateMatch>[Finders.Length];

            for (var i = 0; i < Finders.Length; i++)
            {
                result[i] = Enumerable.Range(startPosition, input.Length).AsParallel().AsOrdered()
                    .Select(x => Finders[i].FindMatchesAtPosition(input, x)).ToArray();
            }

            return result;
        }

        private bool IsFirstLiteralRun(int dataPosition, int unitSize, PositionElement[] history)
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
