using System;
using Kompression.LempelZiv.Encoders;
using Kompression.LempelZiv.MatchFinder;
using Kompression.LempelZiv.Models;

namespace Kompression.LempelZiv.Parser
{
    class OptimalParser : ILzParser
    {
        private readonly ILzMatchFinder _finder;
        private readonly ILengthCalculator _calculator;

        private int[] _price;
        private int[] _len;
        private long[] _dist;

        public OptimalParser(ILzMatchFinder finder, ILengthCalculator calculator)
        {
            _finder = finder;
            _calculator = calculator;
        }

        // TODO: Finish backpass and return value
        public LzMatch[] Parse(Span<byte> input)
        {
            _price = new int[input.Length + 1];
            _len = new int[input.Length + 1];
            _dist = new long[input.Length + 1];

            for (var i = 0; i < input.Length + 1; i++)
                _price[i] = unchecked((int)0xFFFFFFFF);

            _price[0] = 0;

            ForwardPass(input);

            return null;
        }

        private void ForwardPass(Span<byte> input)
        {
            for (var i = 0; i < input.Length; ++i)
            {
                var literalCost = _price[i] + _calculator.CalculateLiteralLength(input[i]);
                if (literalCost < _price[i + 1])
                {
                    _price[i + 1] = literalCost;
                    _len[i + 1] = 1;
                    _dist[i + 1] = 0;
                }

                // Don't try matches close to end of buffer.
                if (i + _finder.MinMatchSize >= input.Length)
                    continue;

                // Get longest match and set price for it
                var match = _finder.FindLongestMatch(input, i);
                if (match != null)
                {
                    var matchCost = _price[i] + _calculator.CalculateMatchLength(match);
                    if (matchCost < _price[i + match.Length])
                    {
                        _price[i + match.Length] = matchCost;
                        _len[i + match.Length] = match.Length;
                        _dist[i + match.Length] = match.Displacement;
                    }
                }
            }
        }

        #region Dispose

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool dispose)
        {
            if (dispose)
                _finder.Dispose();
        }

        #endregion
    }
}
