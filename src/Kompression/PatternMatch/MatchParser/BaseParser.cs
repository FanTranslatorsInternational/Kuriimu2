using System.Collections.Generic;
using System.IO;
using Kompression.Extensions;
using Kontract.Kompression;
using Kontract.Kompression.Model;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.PatternMatch.MatchParser
{
    public abstract class BaseParser : IMatchParser
    {
        protected IMatchFinder[] Finders { get; }

        public FindOptions FindOptions { get; }

        public BaseParser(FindOptions options, params IMatchFinder[] finders)
        {
            FindOptions = options;
            Finders = finders;
        }

        // TODO: Maybe not rely on input position, and set position by manipulators
        public IEnumerable<Match> ParseMatches(Stream input)
        {
            var manipulatedStream = FindOptions.InputManipulator.Manipulate(input);
            var originalBuffer = manipulatedStream.ToArray();

            foreach (var finder in Finders)
                finder.PreProcess(originalBuffer);

            var matches = InternalParseMatches(originalBuffer, (int)manipulatedStream.Position);
            foreach (var match in matches)
            {
                FindOptions.InputManipulator.AdjustMatch(match);
                yield return match;
            }
        }

        protected abstract IEnumerable<Match> InternalParseMatches(byte[] input, int startPosition);

        public virtual void Dispose()
        {
        }
    }
}
