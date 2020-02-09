using System.Collections.Generic;
using System.Linq;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.PatternMatch.MatchParser.Support
{
    class AggregateMatch
    {
        private readonly IList<Match> _matches;
        private readonly (int, int, int)[] _lengthRange;

        public int MaxLength { get; }

        public bool HasMatches { get; }

        public AggregateMatch(IList<Match> matches)
        {
            _matches = matches;
            _lengthRange = new (int, int, int)[matches.Count];

            var prevLength = 1;
            for (var i = 0; i < matches.Count; i++)
            {
                _lengthRange[i] = (prevLength, matches[i].Length, i);
                prevLength = matches[i].Length + 1;
            }

            HasMatches = matches.Any();
            if (HasMatches)
                MaxLength = matches.Last().Length;
        }

        public int GetDisplacement(int length)
        {
            return _matches[_lengthRange.First(x => length >= x.Item1 && length <= x.Item2).Item3].Displacement;
        }
    }
}
