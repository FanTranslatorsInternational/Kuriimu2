using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kompression.Configuration;
using Kompression.Extensions;
using Kompression.Interfaces;
using Kompression.IO;
using Kompression.Models;
using Kompression.PatternMatch.MatchParser.Support;

namespace Kompression.PatternMatch.MatchParser
{
    public class ForwardBackwardOptimalParser : IMatchParser
    {
        private PriceHistoryElement[] _history;
        private IPriceCalculator _priceCalculator;
        private IMatchFinder[] _finders;

        public FindOptions FindOptions { get; }

        public ForwardBackwardOptimalParser(FindOptions options, IPriceCalculator priceCalculator, params IMatchFinder[] finders)
        {
            if (finders.Any(x => x.FindOptions.UnitSize != finders[0].FindOptions.UnitSize))
                throw new InvalidOperationException("All match finder have to have the same unit size.");

            FindOptions = options;
            _priceCalculator = priceCalculator;
            _finders = finders;
        }

        public IEnumerable<Match> ParseMatches(Stream input)
        {
            var toParse = input;

            if (FindOptions.PreBufferSize > 0)
                toParse = FindOptions.SearchBackwards ?
                    new ConcatStream(input, new MemoryStream(new byte[FindOptions.PreBufferSize])) :
                    new ConcatStream(new MemoryStream(new byte[FindOptions.PreBufferSize]), input);

            if (FindOptions.SearchBackwards)
                toParse = new ReverseStream(toParse, toParse.Length);

            return InternalParseMatches(toParse.ToArray(), FindOptions.PreBufferSize);
        }

        private IEnumerable<Match> InternalParseMatches(byte[] input, int startPosition)
        {
            var unitSize = (int)FindOptions.UnitSize;
            _history = new PriceHistoryElement[input.Length - startPosition + unitSize];
            for (int i = 0; i < _history.Length; i += (int)FindOptions.UnitSize)
                _history[i] = new PriceHistoryElement { Price = int.MaxValue };
            _history[0].Price = 0;

            ForwardPass(input, startPosition);
            return BackwardPass(input.Length - startPosition).Reverse().ToArray();
        }

        private void ForwardPass(byte[] input, int startPosition)
        {
            var state = new ParserState(_history, FindOptions);

            var unitSize = (int)FindOptions.UnitSize;
            var dataPosition = 0;
            var dataLength = input.Length - startPosition;
            foreach (var matches in GetAllMatches(input, startPosition))
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
                for (int finderIndex = 0; finderIndex < _finders.Length; finderIndex++)
                {
                    var finderMatches = matches[finderIndex];
                    if (finderMatches == null || !finderMatches.Any())
                        continue;

                    var longestMatch = finderMatches.MaxBy(x => x.Position);

                    for (var j = _finders[finderIndex].FindLimitations.MinLength; j <= longestMatch.Length; j += unitSize)
                    {
                        var matchPrice = _priceCalculator.CalculateMatchPrice(state, dataPosition, longestMatch.Displacement, j);
                        matchPrice += _history[dataPosition].Price;

                        if (dataPosition + j < _history.Length &&
                            matchPrice < _history[dataPosition + j].Price)
                        {
                            _history[dataPosition + j].IsLiteral = false;
                            _history[dataPosition + j].Displacement = longestMatch.Displacement;
                            //finderMatches.TakeWhile(x => x.Length >= j).OrderBy(x => x.Displacement).First().Displacement;
                            _history[dataPosition + j].Length = j;
                            _history[dataPosition + j].Price = matchPrice;
                        }
                    }
                }

                dataPosition += unitSize;
            }
        }

        private IEnumerable<Match> BackwardPass(int dataLength)
        {
            var unitSize = (int)_finders[0].FindOptions.UnitSize;

            for (var i = dataLength + unitSize - 1; i > 0;)
            {
                if (_history[i].IsLiteral)
                    i -= unitSize;
                else
                {
                    yield return new Match(i - _history[i].Length, _history[i].Displacement, _history[i].Length);
                    i -= _history[i].Length + unitSize * FindOptions.SkipUnitsAfterMatch;
                }
            }
        }

        private IEnumerable<Match[][]> GetAllMatches(byte[] input, int startPosition)
        {
            var finderEnumerators = new IEnumerator<Match[]>[_finders.Length];
            for (var i = 0; i < _finders.Length; i++)
                finderEnumerators[i] = _finders[i].GetAllMatches(input, startPosition).GetEnumerator();

            var continueExecution = true;
            while (continueExecution)
            {
                var findersResult = new Match[_finders.Length][];
                for (int finderIndex = 0; finderIndex < _finders.Length; finderIndex++)
                {
                    if (!finderEnumerators[finderIndex].MoveNext())
                    {
                        continueExecution = false;
                        break;
                    }

                    findersResult[finderIndex] = finderEnumerators[finderIndex].Current;
                }

                if (continueExecution)
                    yield return findersResult;
            }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
