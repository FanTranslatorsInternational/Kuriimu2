using System.Collections.Generic;
using System.Linq;

namespace Kontract.Kompression.Model.PatternMatch
{
    public class AggregateMatch
    {
        private readonly (int displacement, int length)[] _matches;

        public int MaxLength => _matches.Last().length;

        public bool HasMatches => _matches.Any();

        public AggregateMatch(IList<(int displacement, int length)> matches)
        {
            _matches = matches.Select(x => (x.displacement, x.length)).ToArray();
        }

        public AggregateMatch(int displacement, int length)
        {
            _matches = new[] { (displacement, length) };
        }

        public int GetDisplacement(int length)
        {
            var minLength = 1;
            for (var i = 0; i < _matches.Length; i++)
            {
                if (length >= minLength && length <= _matches[i].length)
                    return _matches[i].displacement;

                minLength += _matches[i].length;
            }

            return -1;
        }
    }
}
