using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder.LempelZiv.InputManipulation;
using Kompression.Encoder.LempelZiv.InputManipulation.Streams;

namespace Kompression.Encoder.LempelZiv.InputManipulation
{
    internal class PrependInputValue(int preBufferSize, byte value) : IInputManipulation
    {
        public Stream Manipulate(Stream input)
        {
            var newStream = new PreBufferStream(input, preBufferSize, value)
            {
                Position = input.Position + preBufferSize
            };

            return newStream;
        }

        public void AdjustMatch(LempelZivMatch lempelZivMatch)
        {
        }
    }
}
