using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder.LempelZiv.InputManipulation;
using Kompression.Encoder.LempelZiv.InputManipulation.Streams;

namespace Kompression.Encoder.LempelZiv.InputManipulation
{
    internal class PrependInputData : IInputManipulation
    {
        private readonly byte[] _data;

        public PrependInputData(byte[] data)
        {
            _data = data;
        }

        public Stream Manipulate(Stream input)
        {
            var newStream = new PreBufferStream(input, _data)
            {
                Position = input.Position + _data.Length
            };

            return newStream;
        }

        public void AdjustMatch(LempelZivMatch lempelZivMatch)
        {
        }
    }
}
