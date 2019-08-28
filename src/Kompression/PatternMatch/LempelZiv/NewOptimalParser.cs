using System.Collections.Generic;
using System.Linq;
using Kompression.PatternMatch.LempelZiv.Support;

namespace Kompression.PatternMatch.LempelZiv
{
    /// <summary>
    /// Second attempt of an optimal parser, based on the first implementation and dsdecmps reverse engineered official code.
    /// </summary>
    public class NewOptimalParser : IMatchParser
    {
        private PriceHistoryElement[] _history;

        private IPriceCalculator _priceCalculator;
        private IMatchFinder[] _finders;

        public int SkipAfterMatch { get; }

        public NewOptimalParser(IPriceCalculator priceCalculator, int skipAfterMatch = 0, params IMatchFinder[] finders)
        {
            SkipAfterMatch = skipAfterMatch;
            _priceCalculator = priceCalculator;
            _finders = finders;
        }

        /// <inheritdoc cref="ParseMatches"/>
        public Match[] ParseMatches(byte[] input, int startPosition)
        {
            _history = new PriceHistoryElement[input.Length - startPosition];

            BackwardPass(input, startPosition);
            return ForwardPass(input.Length - startPosition).ToArray();
        }

        private void BackwardPass(byte[] input, int startPosition)
        {
            var matches = GetAllMatches(input, startPosition);

            var dataLength = input.Length - startPosition;
            var unitSize = _finders.Min(x => (int)x.DataType);
            for (var i = dataLength - unitSize; i >= 0; i -= unitSize)
            {
                // First get the compression length when the next byte is not compressed
                _history[i] = new PriceHistoryElement
                {
                    Length = unitSize,
                    Price = _priceCalculator.CalculateLiteralPrice(input[i + startPosition])
                };
                if (i + unitSize < dataLength)
                    _history[i].Price += _history[i + unitSize].Price;

                // Then go through all longest matches at position i
                var matchIndex = 0;
                foreach (var match in matches[i])
                {
                    // Get the longest match at position i
                    if (match != null)
                    {
                        var matchLength = match.Length;
                        for (var j = _finders[matchIndex].MinMatchSize; j <= matchLength; j += unitSize)
                        {
                            match.Length = j;
                            var matchPrice = _priceCalculator.CalculateMatchPrice(match);

                            long newCompLen = matchPrice;
                            if (i + j < dataLength)
                                newCompLen += _history[i + j].Price;

                            if (newCompLen < _history[i].Price)
                            {
                                _history[i].Displacement = match.Displacement;
                                _history[i].Length = j;
                                _history[i].Price = newCompLen;
                            }
                        }
                    }

                    matchIndex++;
                }
            }
        }

        private Match[][] GetAllMatches(byte[] input, int startPosition)
        {
            var result = new Match[input.Length - startPosition][];

            var dataLength = input.Length - startPosition;
            var unitSize = _finders.Min(x => (int)x.DataType);
            for (var i = 0; i < dataLength; i += unitSize)
            {
                result[i] = new Match[_finders.Length];
                var foundMatches = 0;

                foreach (var finder in _finders)
                {
                    var match = finder.FindMatches(input, i + startPosition).FirstOrDefault();
                    if (match != null)
                    {
                        result[i][foundMatches++] = match;
                    }
                }
            }

            return result;
        }

        private IEnumerable<Match> ForwardPass(int dataLength)
        {
            var unitSize = _finders.Min(x => (int)x.DataType);
            for (var i = 0; i < dataLength;)
            {
                if (_history[i].Length == 1)
                    i += unitSize;
                else
                {
                    yield return new Match(i, _history[i].Displacement, _history[i].Length);
                    i += (int)_history[i].Length + unitSize * SkipAfterMatch;
                }
            }
        }

        public void Dispose()
        {
            foreach (var finder in _finders)
            {
                finder.Dispose();
            }

            _priceCalculator = null;
            _finders = null;
            _history = null;
        }
    }
}
