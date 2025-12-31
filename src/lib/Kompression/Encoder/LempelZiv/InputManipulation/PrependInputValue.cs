using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder.LempelZiv.InputManipulation;
using Kompression.Encoder.LempelZiv.InputManipulation.Streams;

namespace Kompression.Encoder.LempelZiv.InputManipulation
{
    internal class PrependInputValue : IInputManipulation
    {
        private readonly int _preBufferSize;
        private readonly byte _value;

        public PrependInputValue(int preBufferSize, byte value)
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

        public void AdjustMatch(LempelZivMatch lempelZivMatch)
        {
        }
    }
}
