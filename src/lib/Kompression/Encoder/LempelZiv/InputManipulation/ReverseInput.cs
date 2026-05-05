using Komponent.Streams;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder.LempelZiv.InputManipulation;

namespace Kompression.Encoder.LempelZiv.InputManipulation
{
    internal class ReverseInput : IInputManipulation
    {
        private long _streamLength;

        public Stream Manipulate(Stream input)
        {
            _streamLength = input.Length;
            return new ReverseStream(input, input.Length);
        }

        public void AdjustMatch(LempelZivMatch lempelZivMatch)
        {
            lempelZivMatch.SetPosition((int)(_streamLength - lempelZivMatch.Position));
        }
    }
}
