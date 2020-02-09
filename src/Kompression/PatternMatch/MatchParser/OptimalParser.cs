using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kompression.Extensions;
using Kompression.IO;
using Kompression.IO.Streams;
using Kompression.PatternMatch.MatchParser.Support;
using Kontract.Kompression;
using Kontract.Kompression.Model;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.PatternMatch.MatchParser
{
    public class OptimalParser : IMatchParser
    {
        private PositionElement[] _history;

        private readonly IPriceCalculator _priceCalculator;
        private readonly IMatchFinder[] _finders;

        public FindOptions FindOptions { get; }

        public OptimalParser(FindOptions options, IPriceCalculator priceCalculator, params IMatchFinder[] finders)
        {
            if (finders.Any(x => x.FindOptions.UnitSize != finders[0].FindOptions.UnitSize))
                throw new InvalidOperationException("All Match finder have to have the same unit size.");

            FindOptions = options;
            _priceCalculator = priceCalculator;
            _finders = finders;
        }

        public IEnumerable<Match> ParseMatches(Stream input)
        {
            if (FindOptions.PreBufferSize > 0)
                input = new PreBufferStream(input, FindOptions.PreBufferSize);

            if (FindOptions.SearchBackwards)
                input = new ReverseStream(input, input.Length);

            return InternalParseMatches(input.ToArray(), FindOptions.PreBufferSize);
        }

        private IEnumerable<Match> InternalParseMatches(byte[] input, int startPosition)
        {
            foreach (var finder in _finders)
                finder.PreProcess(input, startPosition);

            _history = new PositionElement[input.Length - startPosition + 1];
            for (var i = 0; i < _history.Length; i++)
                _history[i] = new PositionElement(0, false, null, int.MaxValue);
            _history[0].Price = 0;

            ForwardPass(input, startPosition);
            return BackwardPass().Reverse();
        }

        private void ForwardPass(byte[] input, int startPosition)
        {
            //var state = new ParserState(_history);

            var matches = GetAllMatches(input, startPosition);

            var unitSize = (int)FindOptions.UnitSize;
            for (var dataPosition = 0; dataPosition < input.Length - startPosition; dataPosition += unitSize)
            {
                // Calculate literal place at position
                var element = _history[dataPosition];
                var newRunLength = element.IsMatchRun ? unitSize : element.CurrentRunLength + unitSize;
                var isFirstLiteralRun = IsFirstLiteralRun(dataPosition, unitSize);
                var literalPrice = _priceCalculator.CalculateLiteralPrice(input[dataPosition], newRunLength, isFirstLiteralRun);
                literalPrice += element.Price;

                if (dataPosition + unitSize < _history.Length &&
                    literalPrice <= _history[dataPosition + unitSize].Price)
                {
                    var nextElement = _history[dataPosition + unitSize];

                    nextElement.Parent = element;
                    nextElement.Price = literalPrice;
                    nextElement.CurrentRunLength = newRunLength;
                    nextElement.IsMatchRun = false;
                    nextElement.Match = null;
                }

                // Then go through all longest matches at current position
                for (var finderIndex = 0; finderIndex < _finders.Length; finderIndex++)
                {
                    var finderMatch = matches[finderIndex][dataPosition];
                    if (finderMatch == null || !finderMatch.HasMatches)
                        continue;

                    for (var j = _finders[finderIndex].FindLimitations.MinLength; j <= finderMatch.MaxLength; j += unitSize)
                    {
                        var displacement = finderMatch.GetDisplacement(j);
                        newRunLength = element.IsMatchRun ? element.CurrentRunLength + 1 : 1;
                        var matchPrice = _priceCalculator.CalculateMatchPrice(displacement, j, newRunLength);
                        matchPrice += element.Price;

                        if (dataPosition + j < _history.Length &&
                            matchPrice < _history[dataPosition + j].Price)
                        {
                            var nextElement = _history[dataPosition + j];

                            nextElement.Parent = element;
                            nextElement.Price = matchPrice;
                            nextElement.CurrentRunLength = newRunLength;
                            nextElement.IsMatchRun = true;
                            nextElement.Match = new Match(dataPosition + FindOptions.PreBufferSize, displacement, j);
                        }
                    }
                }
            }
        }

        private IEnumerable<Match> BackwardPass()
        {
            var element = _history.Last();
            while (element != null)
            {
                if (element.Match.HasValue)
                    yield return element.Match.Value;

                element = element.Parent;
            }
        }

        private IList<IList<AggregateMatch>> GetAllMatches(byte[] input, int startPosition)
        {
            var result = new IList<AggregateMatch>[_finders.Length];

            for (var i = 0; i < _finders.Length; i++)
            {
                result[i] = Enumerable.Range(startPosition, input.Length).AsParallel().AsOrdered()
                    .Select(x => new AggregateMatch(_finders[i].FindMatchesAtPosition(input, x))).ToArray();
            }

            return result;
        }

        private bool IsFirstLiteralRun(int dataPosition, int unitSize)
        {
            while (dataPosition >= 0)
            {
                if (_history[dataPosition].Match != null)
                    return false;

                dataPosition -= unitSize;
            }

            return true;
        }

        public void Dispose()
        {
        }
    }
}
