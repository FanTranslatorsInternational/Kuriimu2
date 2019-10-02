using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kompression.Configuration;
using Kompression.Extensions;
using Kompression.Interfaces;
using Kompression.IO;
using Kompression.MatchParser.Support;
using Kompression.Models;
using MoreLinq;

namespace Kompression.MatchParser
{
    /// <summary>
    /// Parse matches from all positions with a pricing table.
    /// </summary>
    public class BackwardForwardOptimalParser : IMatchParser
    {
        private PriceHistoryElement[] _history;

        private IPriceCalculator _priceCalculator;
        private IMatchFinder[] _finders;

        /// <inheritdoc cref="FindOptions"/>
        public FindOptions FindOptions { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="BackwardForwardOptimalParser"/>.
        /// </summary>
        /// <param name="options">The options to parse pattern matches.</param>
        /// <param name="priceCalculator">The <see cref="IPriceCalculator"/> for match and literal pricing.</param>
        /// <param name="finders">The <see cref="IMatchFinder"/>s to find the longest pattern matches.</param>
        public BackwardForwardOptimalParser(FindOptions options, IPriceCalculator priceCalculator, params IMatchFinder[] finders)
        {
            if (finders.Any(x => x.FindOptions.UnitSize != finders[0].FindOptions.UnitSize))
                throw new InvalidOperationException("All match finder have to have the same unit size.");

            FindOptions = options;
            _priceCalculator = priceCalculator;
            _finders = finders;
        }

        /// <inheritdoc cref="ParseMatches"/>
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
            _history = new PriceHistoryElement[input.Length - startPosition];
            for (int i = 0; i < _history.Length; i += (int)FindOptions.UnitSize)
                _history[i] = new PriceHistoryElement();

            BackwardPass(input, startPosition);
            return ForwardPass(input.Length - startPosition).ToArray();
        }

        private void BackwardPass(byte[] input, int startPosition)
        {
            // ForwardPass test
            //var matches = GetAllMatches(input, startPosition);

            var unitSize = (int)_finders[0].FindOptions.UnitSize;
            var dataLength = input.Length - startPosition;
            var loopStart = dataLength - dataLength % unitSize - unitSize;

            var state = new ParserState(_history, FindOptions);

            var dataPosition = loopStart;
            foreach (var matches in GetAllMatches(input, startPosition))
            {
                // First get the compression length when the next byte is not compressed
                _history[dataPosition] = new PriceHistoryElement
                {
                    IsLiteral = true,
                    Length = unitSize,
                    Price = _priceCalculator.CalculateLiteralPrice(state, dataPosition, input[dataPosition + startPosition])
                };
                if (dataPosition + unitSize < dataLength)
                    _history[dataPosition].Price += _history[dataPosition + unitSize].Price;

                // Then go through all longest matches at position i
                for (int finderIndex = 0; finderIndex < _finders.Length; finderIndex++)
                {
                    var finderMatches = matches[finderIndex];
                    if (finderMatches == null || !finderMatches.Any())
                        continue;

                    var longestMatch = finderMatches.MaxBy(x => x.Position);

                    for (var j = _finders[finderIndex].FindLimitations.MinLength; j <= longestMatch.Length; j += unitSize)
                    {
                        var matchPrice = _priceCalculator.CalculateMatchPrice(state, dataPosition, longestMatch.Displacement, j);

                        var newCompLen = matchPrice;
                        if (dataPosition + j < dataLength)
                            newCompLen += _history[dataPosition + j].Price;

                        if (newCompLen < _history[dataPosition].Price)
                        {
                            _history[dataPosition].IsLiteral = false;
                            _history[dataPosition].Displacement = longestMatch.Displacement;
                            //finderMatches.TakeWhile(x => x.Length >= j).OrderBy(x => x.Displacement).First().Displacement;
                            _history[dataPosition].Length = j;
                            _history[dataPosition].Price = newCompLen;
                        }
                    }
                }

                dataPosition -= unitSize;
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

        private IEnumerable<Match> ForwardPass(int dataLength)
        {
            var unitSize = (int)_finders[0].FindOptions.UnitSize;

            for (var i = 0; i < dataLength;)
            {
                if (_history[i].IsLiteral)
                    i += unitSize;
                else
                {
                    yield return new Match(i, _history[i].Displacement, _history[i].Length);
                    i += (int)_history[i].Length + unitSize * FindOptions.SkipUnitsAfterMatch;
                }
            }
        }

        public void Dispose()
        {
            foreach (var finder in _finders)
                finder?.Dispose();

            _priceCalculator = null;
            _finders = null;
            FindOptions = null;
        }
    }
}
