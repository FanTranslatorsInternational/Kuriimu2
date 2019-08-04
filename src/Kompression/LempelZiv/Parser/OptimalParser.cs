using System;
using System.Collections.Generic;
using System.Linq;
using Kompression.LempelZiv.MatchFinder;
using Kompression.LempelZiv.PriceCalculators;

namespace Kompression.LempelZiv.Parser
{
    class OptimalParser : ILzParser
    {
        private readonly IAllMatchFinder _finder;
        private readonly IPriceCalculator _calculator;

        private int[] _price;
        private int[] _len;
        private long[] _dist;

        public OptimalParser(IAllMatchFinder finder, IPriceCalculator calculator)
        {
            _finder = finder;
            _calculator = calculator;
        }

        public LzMatch[] Parse(Span<byte> input)
        {
            _price = new int[input.Length + 1];
            _len = new int[input.Length + 1];
            _dist = new long[input.Length + 1];

            for (var i = 0; i < input.Length + 1; i++)
                _price[i] = 999999999;

            _price[0] = 0;

            ForwardPass(input);
            return BackwardPass(input);
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

                // Get all matches and set prices for each
                var matches = _finder.FindAllMatches(input, i);
                foreach (var match in matches)
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

        private LzMatch[] BackwardPass(Span<byte> input)
        {
            var results = new List<LzMatch>();

            for (var i = input.Length; i > 0; i--)
            {
                if (_len[i] > 1)
                {
                    results.Add(new LzMatch(i - _len[i], _dist[i], _len[i]));
                    i -= _len[i];
                }
            }

            return results.OrderBy(x => x.Position).ToArray();
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
