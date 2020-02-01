using Kompression.Configuration;
using Kompression.Interfaces;

namespace Kompression.PatternMatch.MatchParser.Support
{
    class ParserState : IMatchState
    {
        private PriceHistoryElement[] _history;

        public ParserState(PriceHistoryElement[] history)
        {
            _history = history;
        }

        public int CountLiterals(int position)
        {
            var literalCount = 0;
            while (position >= 0 && _history[position].IsLiteral)
            {
                literalCount++;
                position -= _history[position].Length;
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

        public bool HasMatches(int position)
        {
            while (position >= 0)
            {
                if (!_history[position].IsLiteral)
                    return true;

                position -= _history[position].Length;
            }

            return false;
        }

        public void Dispose()
        {
            _history = null;
        }
    }
}
