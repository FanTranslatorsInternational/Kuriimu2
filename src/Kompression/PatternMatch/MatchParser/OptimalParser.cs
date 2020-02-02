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
        private PriceHistoryElement[] _history;

        private readonly IPriceCalculator _priceCalculator;
        private readonly IMatchFinder[] _finders;

        public FindOptions FindOptions { get; }

        public OptimalParser(FindOptions options, IPriceCalculator priceCalculator, params IMatchFinder[] finders)
        {
            if (finders.Any(x => x.FindOptions.UnitSize != finders[0].FindOptions.UnitSize))
                throw new InvalidOperationException("All match finder have to have the same unit size.");

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

            _history = Enumerable.Repeat(new PriceHistoryElement { Price = int.MaxValue }, input.Length - startPosition + 1).ToArray();
            _history[0].Price = 0;

            ForwardPass(input, startPosition);
            return BackwardPass(input.Length - startPosition).Reverse().ToArray();
        }

        private void ForwardPass(byte[] input, int startPosition)
        {
            var state = new ParserState(_history);

            var matches = GetAllMatches(input, startPosition);

            var unitSize = (int)FindOptions.UnitSize;
            for (var dataPosition = 0; dataPosition < input.Length - startPosition; dataPosition += unitSize)
            {
                // Calculate literal place at position
                var literalPrice = _priceCalculator.CalculateLiteralPrice(state, dataPosition, input[dataPosition]);
                literalPrice += _history[dataPosition].Price;
                if (dataPosition + unitSize < _history.Length &&
                    literalPrice < _history[dataPosition + unitSize].Price)
                {
                    _history[dataPosition + unitSize].IsLiteral = true;
                    _history[dataPosition + unitSize].Price = literalPrice;
                    _history[dataPosition + unitSize].Length = unitSize;
                    _history[dataPosition + unitSize].Displacement = 0;
                }

                // Then go through all longest matches at current position
                for (var finderIndex = 0; finderIndex < _finders.Length; finderIndex++)
                {
                    var finderMatches = matches[finderIndex][dataPosition];
                    if (finderMatches == null || !finderMatches.Any())
                        continue;

                    var matchIndex = 0;
                    for (var j = _finders[finderIndex].FindLimitations.MinLength; j <= finderMatches[finderMatches.Count - 1].Length; j += unitSize)
                    {
                        var matchPrice = _priceCalculator.CalculateMatchPrice(state, dataPosition, finderMatches[matchIndex].Displacement, j);
                        matchPrice += _history[dataPosition].Price;

                        if (dataPosition + j < _history.Length &&
                            matchPrice < _history[dataPosition + j].Price)
                        {
                            _history[dataPosition + j].IsLiteral = false;
                            _history[dataPosition + j].Displacement = finderMatches[matchIndex].Displacement;
                            _history[dataPosition + j].Length = j;
                            _history[dataPosition + j].Price = matchPrice;
                        }

                        if (j + unitSize > finderMatches[matchIndex].Length)
                            matchIndex++;
                    }
                }
            }
        }

        private IEnumerable<Match> BackwardPass(int dataLength)
        {
            var unitSize = (int)_finders[0].FindOptions.UnitSize;

            for (var i = dataLength; i > 0;)
            {
                if (_history[i].IsLiteral)
                    i -= unitSize;
                else
                {
                    yield return new Match(i - _history[i].Length + FindOptions.PreBufferSize, _history[i].Displacement, _history[i].Length);
                    i -= _history[i].Length + unitSize * FindOptions.SkipUnitsAfterMatch;
                }
            }
        }

        private IList<IList<IList<Match>>> GetAllMatches(byte[] input, int startPosition)
        {
            var result = new IList<IList<Match>>[_finders.Length];

            for (var i = 0; i < _finders.Length; i++)
            {
                result[i] = Enumerable.Range(startPosition, input.Length).AsParallel().AsOrdered()
                    .Select(x => _finders[i].FindMatchesAtPosition(input, x)).ToArray();
            }

            return result;
        }

        public void Dispose()
        {
        }
    }
}
