using Kompression.Contract.Configuration;
using Kompression.DataClasses.Configuration;
using Kompression.Encoder.LempelZiv.InputManipulation;

namespace Kompression.Configuration
{
    internal class LempelZivInputAdjustmentOptionsBuilder(LempelZivInputAdjustmentOptions options)
        : ILempelZivInputAdjustmentOptionsBuilder
    {
        public ILempelZivInputAdjustmentOptionsBuilder Skip(int skip)
        {
            options.InputManipulations.Add(new SkipInput(skip));
            return this;
        }

        public ILempelZivInputAdjustmentOptionsBuilder Reverse()
        {
            options.InputManipulations.Add(new ReverseInput());
            return this;
        }

        public ILempelZivInputAdjustmentOptionsBuilder Prepend(int byteCount, byte value)
        {
            options.InputManipulations.Add(new PrependInputValue(byteCount, value));
            return this;
        }

        public ILempelZivInputAdjustmentOptionsBuilder Prepend(byte[] data)
        {
            options.InputManipulations.Add(new PrependInputData(data));
            return this;
        }
    }
}
