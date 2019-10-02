using Kompression.Configuration;
using Kompression.Interfaces;

namespace Kompression.MatchParser.Support
{
    class ParserState : IMatchState
    {
        private PriceHistoryElement[] _history;
        private FindOptions _findOptions;

        public ParserState(PriceHistoryElement[] history, FindOptions findOptions)
        {
            _history = history;
            _findOptions = findOptions;
        }

        public int CountLiterals(int position)
        {
            var literalCount = 0;
            while (position >= 0 && _history[position].IsLiteral)
            {
                literalCount++;
                position -= (int)_findOptions.UnitSize;
            }

            return literalCount;
        }

        public int CountMatches(int position)
        {
            var matchCount = 0;
            while (position >= 0 && !_history[position].IsLiteral)
            {
                matchCount++;
                position -= _history[position].Length;
            }

            return matchCount;
        }

        public void Dispose()
        {
            _history = null;
            _findOptions = null;
        }
    }
}
