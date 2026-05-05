using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Encoder.LempelZiv.InputManipulation;
using Kompression.DataClasses.Configuration;

namespace Kompression.Encoder.LempelZiv.InputManipulation
{
    internal class InputManipulator(LempelZivInputAdjustmentOptions options) : IInputManipulator
    {
        public Stream Manipulate(Stream input)
        {
            foreach (IInputManipulation manipulation in options.InputManipulations)
                input = manipulation.Manipulate(input);

            return input;
        }

        public void AdjustMatch(LempelZivMatch match)
        {
            foreach (IInputManipulation manipulation in options.InputManipulations.Reverse())
                manipulation.AdjustMatch(match);
        }
    }
}
