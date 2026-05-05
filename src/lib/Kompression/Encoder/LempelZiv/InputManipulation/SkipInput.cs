using Komponent.Streams;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder.LempelZiv.InputManipulation;

namespace Kompression.Encoder.LempelZiv.InputManipulation
{
    internal class SkipInput(int skip) : IInputManipulation
    {
        public Stream Manipulate(Stream input)
        {
            return new SubStream(input, skip, input.Length - skip)
            {
                Position = Math.Max(0, input.Position - skip)
            };
        }

        public void AdjustMatch(LempelZivMatch lempelZivMatch)
        {
            lempelZivMatch.SetPosition(lempelZivMatch.Position + skip);
        }
    }
}
