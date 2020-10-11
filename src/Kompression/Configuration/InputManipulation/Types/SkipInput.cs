using System.IO;
using Komponent.IO.Streams;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.Configuration.InputManipulation.Types
{
    class SkipInput : IInputManipulationType
    {
        private readonly int _skip;

        public SkipInput(int skip)
        {
            _skip = skip;
        }

        public Stream Manipulate(Stream input)
        {
            return new SubStream(input, _skip, input.Length - _skip)
            {
                Position = input.Position - _skip
            };
        }

        public void AdjustMatch(Match match)
        {
            match.SetPosition(match.Position - _skip);
        }
    }
}
