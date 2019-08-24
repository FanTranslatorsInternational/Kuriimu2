using System;
using System.Collections.Generic;
using System.Linq;
using Kompression.LempelZiv.MatchFinder;
using Kompression.LempelZiv.Parser.Models;
using Kompression.LempelZiv.PriceCalculators;

namespace Kompression.LempelZiv.Parser
{
    /// <summary>
    /// Searches for the most optimal distribution of pattern matches.
    /// </summary>
    public class OptimalParser : ILzParser
    {
        private IAllMatchFinder[] _finders;
        private IPriceCalculator _calculator;
        private PriceHistoryElement[] _priceHistory;

        public OptimalParser(IPriceCalculator calculator, params IAllMatchFinder[] finders)
        {
            _calculator = calculator;
            _finders = finders;
        }

        /// <inheritdoc cref="Parse"/>
        public Match[] Parse(byte[] input, int startPosition)
        {
            InitializePriceHistory(input.Length - startPosition + 1);
            ForwardPass(input, startPosition);
            return BackwardPass(input, startPosition);
        }

        /// <summary>
        /// Initializes the price history table.
        /// </summary>
        /// <param name="historyLength">The length of the table.</param>
        private void InitializePriceHistory(int historyLength)
        {
            _priceHistory = new PriceHistoryElement[historyLength];
            for (var i = 0; i < _priceHistory.Length; i++)
                _priceHistory[i] = new PriceHistoryElement();

            _priceHistory[0].Price = 0;
        }

        /// <summary>
        /// The first pass through the input, calculating the prices of all found pattern matches.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <param name="startPosition">The position to start at in the input data.</param>
        private void ForwardPass(byte[] input, int startPosition)
        {
            var minMatchSize = _finders.Min(x => x.MinMatchSize);
            for (var i = 0; i < input.Length - startPosition; i++)
            {
                // Calculate literal price
                var literalCost = _priceHistory[i].Price + _calculator.CalculateLiteralLength(input[startPosition + i]);
                if (_priceHistory[i + 1].Price < 0 || literalCost < _priceHistory[i + 1].Price)
                {
                    _priceHistory[i + 1].Price = literalCost;
                    _priceHistory[i + 1].Displacement = 0;
                    _priceHistory[i + 1].Length = 1;
                }

                // Don't try matches close to end of buffer.
                if (startPosition + i + minMatchSize > input.Length)
                    continue;

                // Get all matches and set prices for each
                foreach (var finder in _finders)
                {
                    var matches = finder.FindAllMatches(input, startPosition + i);
                    var matchSet = false;
                    foreach (var match in matches)
                    {
                        var matchCost = _priceHistory[i].Price + _calculator.CalculateMatchLength(match);
                        var priceEntry = _priceHistory[i + match.Length];
                        if (priceEntry.Price < 0 || matchCost < priceEntry.Price)
                        {
                            priceEntry.Price = matchCost;
                            priceEntry.Length = match.Length;
                            priceEntry.Displacement = match.Displacement;

                            // This switch is to disallow future replacement of displacement and length,
                            // if the previous match doesn't belong to this position
                            matchSet = true;
                        }
                        else if (matchCost == priceEntry.Price && matchSet)
                        {
                            // Otherwise code executed here will replace match data that isn't associated to the current position
                            // Which leads to a wrong pattern stored
                            if (priceEntry.Length == match.Length)
                            {
                                priceEntry.Displacement = Math.Min(match.Displacement, priceEntry.Displacement);
                            }
                            else
                            {
                                if (match.Length > priceEntry.Length)
                                {
                                    priceEntry.Length = match.Length;
                                    priceEntry.Displacement = match.Displacement;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Second pass through the input, collecting all relevant matches based on the set prices in first pass.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <param name="startPosition">The position to start at in the input data.</param>
        /// <returns></returns>
        private Match[] BackwardPass(byte[] input, int startPosition)
        {
            var results = new List<Match>();

            var minUnitLength = _finders.Min(x => (int)x.UnitLength);
            for (var i = input.Length - startPosition; i > 0;)
            {
                if (_priceHistory[i].Length > 1)
                {
                    results.Add(new Match(startPosition + i - _priceHistory[i].Length, _priceHistory[i].Displacement, _priceHistory[i].Length));
                    i -= (int)_priceHistory[i].Length;
                }
                else
                {
                    i -= minUnitLength;
                }
            }

            results.Reverse();
            return results.ToArray();
        }

        #region Dispose

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool dispose)
        {
            if (dispose)
            {
                foreach (var finder in _finders)
                    finder.Dispose();

                _finders = null;
                _calculator = null;

                Array.Clear(_priceHistory, 0, _priceHistory.Length);
                _priceHistory = null;
            }
        }

        #endregion
    }
}
