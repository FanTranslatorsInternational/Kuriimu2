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

namespace Kompression.MatchParser
{
    /// <summary>
    /// Parse matches from all positions with a pricing table.
    /// </summary>
    public class OptimalParser : IMatchParser
    {
        private PriceHistoryElement[] _history;

        private IPriceCalculator _priceCalculator;
        private IMatchFinder[] _finders;

        /// <inheritdoc cref="FindOptions"/>
        public FindOptions FindOptions { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="OptimalParser"/>.
        /// </summary>
        /// <param name="options">The options to parse pattern matches.</param>
        /// <param name="priceCalculator">The <see cref="IPriceCalculator"/> for match and literal pricing.</param>
        /// <param name="finders">The <see cref="IMatchFinder"/>s to find the longest pattern matches.</param>
        public OptimalParser(FindOptions options, IPriceCalculator priceCalculator, params IMatchFinder[] finders)
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

            BackwardPass(input, startPosition);
            return ForwardPass(input.Length - startPosition).ToArray();
        }

        private void BackwardPass(byte[] input, int startPosition)
        {
            var matches = GetAllMatches(input, startPosition);

            var unitSize = (int)_finders[0].FindOptions.UnitSize;
            var dataLength = input.Length - startPosition;
            var loopStart = dataLength - (dataLength % unitSize) - unitSize;
            for (var dataPosition = loopStart; dataPosition >= 0; dataPosition -= unitSize)
            {
                // First get the compression length when the next byte is not compressed
                _history[dataPosition] = new PriceHistoryElement
                {
                    IsLiteral = true,
                    Length = unitSize,
                    Price = _priceCalculator.CalculateLiteralPrice(input[dataPosition + startPosition])
                };
                if (dataPosition + unitSize < dataLength)
                    _history[dataPosition].Price += _history[dataPosition + unitSize].Price;

                // Then go through all longest matches at position i
                for (int finderIndex = 0; finderIndex < _finders.Length; finderIndex++)
                {
                    var finderMatches = matches[dataPosition][finderIndex];
                    if (finderMatches != null)
                    {
                        // Due to descending order by length done by GetAllMatches, the first element is the longest match
                        var longestMatch = finderMatches.First();

                        var matchLength = longestMatch.Length;
                        for (var j = _finders[finderIndex].FindLimitations.MinLength; j <= matchLength; j += unitSize)
                        {
                            longestMatch.Length = j;
                            var matchPrice = _priceCalculator.CalculateMatchPrice(longestMatch);

                            long newCompLen = matchPrice;
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
                }
            }
        }

        private Match[][][] GetAllMatches(byte[] input, int startPosition)
        {
            var matches = new Match[input.Length - startPosition][][];
            for (var i = 0; i < matches.Length; i++)
                matches[i] = new Match[_finders.Length][];

            for (int finderIndex = 0; finderIndex < _finders.Length; finderIndex++)
            {
                var finderMatches = _finders[finderIndex].GetAllMatches(input, startPosition).ToArray();

                var groupedMatches = finderMatches.GroupBy(x => x.Position);
                foreach (var matchGroup in groupedMatches)
                    matches[matchGroup.Key][finderIndex] = matchGroup.OrderByDescending(x => x.Length).ToArray();
            }

            return matches;
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
