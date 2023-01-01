using System.IO;
using Kompression.IO.Streams;
using Kontract.Kompression.Models.PatternMatch;

namespace Kompression.Configuration.InputManipulation.Types
{
    class PrependInput : IInputManipulationType
    {
        private readonly int _preBufferSize;
        private readonly byte _value;

        public PrependInput(int preBufferSize, byte value)
        {
            _preBufferSize = preBufferSize;
            _value = value;
        }

        public Stream Manipulate(Stream input)
        {
            var newStream = new PreBufferStream(input, _preBufferSize, _value)
            {
                Position = input.Position + _preBufferSize
            };

            return newStream;
        }

        public void AdjustMatch(Match match)
        {
        }
    }
}
