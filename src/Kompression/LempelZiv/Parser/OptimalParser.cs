using System;
using System.Collections.Generic;
using System.Linq;
using Kompression.LempelZiv.Encoders;
using Kompression.LempelZiv.MatchFinder;
using Kompression.LempelZiv.PriceCalculators;

namespace Kompression.LempelZiv.Parser
{
    class OptimalParser : ILzParser
    {
        private readonly ILzMatchFinder _finder;
        private readonly IPriceCalculator _calculator;

        private int[] _price;
        private int[] _len;
        private long[] _dist;

        public OptimalParser(ILzMatchFinder finder, IPriceCalculator calculator)
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

            //var numSeq = 0;
            //var numLit = 0;

            // Backward pass, pick best option at each step.
            //if (_len[input.Length] <= 1)
            //{
            //    _matchDist[numSeq] = 0;
            //    _matchLen[numSeq] = 0;
            //    _literalLen[numSeq] = 0;
            //    numSeq++;
            //}

            for (var i = input.Length; i > 0; i--)
            {
                if (_len[i] > 1)
                {
                    results.Add(new LzMatch(i - _len[i], _dist[i], _len[i]));
                    //_matchDist[numSeq] = _dist[i];
                    //_matchLen[numSeq] = _len[i];
                    //_literalLen[numSeq] = 0;
                    //numSeq++;
                    i -= _len[i];
                }
                //else
                //{
                //    _literals[numLit++] = input[i - 1];
                //    ++_literalLen[numSeq - 1];
                //    i--;
                //}
            }

            return results.OrderBy(x => x.Position).ToArray();

            //_literalLen = _literalLen.Take(numSeq).Reverse().ToArray();
            //_matchDist = _matchDist.Take(numSeq).Reverse().ToArray();
            //_matchLen = _matchLen.Take(numSeq).Reverse().ToArray();
            //_literals = _literals.Take(numLit).Reverse().ToArray();
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
