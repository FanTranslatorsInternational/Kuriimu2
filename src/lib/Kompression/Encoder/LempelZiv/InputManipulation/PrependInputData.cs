using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder.LempelZiv.InputManipulation;
using Kompression.Encoder.LempelZiv.InputManipulation.Streams;

namespace Kompression.Encoder.LempelZiv.InputManipulation
{
    internal class PrependInputData(byte[] data) : IInputManipulation
    {
        public Stream Manipulate(Stream input)
        {
            var newStream = new PreBufferStream(input, data)
            {
                Position = input.Position + data.Length
            };

            return newStream;
        }

        public void AdjustMatch(LempelZivMatch lempelZivMatch)
        {
        }
    }
}
