using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.DataClasses.Encoder.LempelZiv.MatchParser;
using Kompression.Contract.Encoder.LempelZiv.MatchFinder;
using Kompression.Contract.Encoder.LempelZiv.MatchParser;
using Kompression.Extensions;

namespace Kompression.Encoder.LempelZiv.MatchParser
{
    public abstract class LempelZivMatchParser(LempelZivMatchParserOptions options) : ILempelZivMatchParser
    {
        public LempelZivMatchParserOptions Options { get; } = options;

        public IEnumerable<LempelZivMatch> ParseMatches(Stream input)
        {
            Stream manipulatedStream = Options.InputManipulation.Manipulate(input);
            byte[] originalBuffer = manipulatedStream.ToArray();

            foreach (ILempelZivMatchFinder finder in Options.MatchFinders)
                finder.PreProcess(originalBuffer);

            IEnumerable<LempelZivMatch> matches = InternalParseMatches(originalBuffer, (int)manipulatedStream.Position);
            foreach (LempelZivMatch match in matches)
            {
                Options.InputManipulation.AdjustMatch(match);
                yield return match;
            }
        }

        protected abstract IEnumerable<LempelZivMatch> InternalParseMatches(byte[] input, int startPosition);

        public virtual void Dispose()
        {
        }
    }
}
