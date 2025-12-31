using Kompression.Contract.Configuration;
using Kompression.DataClasses.Configuration;
using Kompression.Encoder.LempelZiv.InputManipulation;

namespace Kompression.Configuration
{
    internal class LempelZivInputAdjustmentOptionsBuilder : ILempelZivInputAdjustmentOptionsBuilder
    {
        private readonly LempelZivInputAdjustmentOptions _options;

        public LempelZivInputAdjustmentOptionsBuilder(LempelZivInputAdjustmentOptions options)
        {
            _options = options;
        }

        public ILempelZivInputAdjustmentOptionsBuilder Skip(int skip)
        {
            _options.InputManipulations.Add(new SkipInput(skip));
            return this;
        }

        public ILempelZivInputAdjustmentOptionsBuilder Reverse()
        {
            _options.InputManipulations.Add(new ReverseInput());
            return this;
        }

        public ILempelZivInputAdjustmentOptionsBuilder Prepend(int byteCount, byte value)
        {
            _options.InputManipulations.Add(new PrependInputValue(byteCount, value));
            return this;
        }

        public ILempelZivInputAdjustmentOptionsBuilder Prepend(byte[] data)
        {
            _options.InputManipulations.Add(new PrependInputData(data));
            return this;
        }
    }
}
