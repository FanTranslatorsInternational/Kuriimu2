using System;
using System.IO;
using Kompression.LempelZiv.Matcher.Models;
using Kompression.LempelZiv.Models;

namespace Kompression.LempelZiv.MatchFinder
{
    public class SuffixTreeMatcher : ILzMatchFinder
    {
        private readonly SuffixTree _tree;

        public int MinMatchSize { get; }
        public int MaxMatchSize { get; }

        public SuffixTreeMatcher(int minMatchSize, int maxMatchSize)
        {
            _tree = new SuffixTree();

            MinMatchSize = minMatchSize;
            MaxMatchSize = maxMatchSize;
        }

        // TODO: Implement finding matches at certain position
        public LzMatch[] FindMatches(Span<byte> input, int position)
        {
            throw new NotImplementedException();
        }

        #region Dispose

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
                _tree.Dispose();
        }

        #endregion
    }
}
