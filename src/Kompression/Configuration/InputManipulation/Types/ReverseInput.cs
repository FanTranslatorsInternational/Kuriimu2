using System.IO;
using Komponent.IO.Streams;
using Kontract.Kompression.Model.PatternMatch;

namespace Kompression.Configuration.InputManipulation.Types
{
    class ReverseInput : IInputManipulationType
    {
        private long _streamLength;

        public Stream Manipulate(Stream input)
        {
            _streamLength = input.Length;
            return new ReverseStream(input, input.Length);
        }

        public void AdjustMatch(Match match)
        {
            match.SetPosition((int)(_streamLength - match.Position));
        }
    }
}
