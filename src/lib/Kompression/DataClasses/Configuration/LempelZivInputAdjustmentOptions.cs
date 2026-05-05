using Kompression.Contract.Encoder.LempelZiv.InputManipulation;

namespace Kompression.DataClasses.Configuration
{
    internal class LempelZivInputAdjustmentOptions
    {
        public IList<IInputManipulation> InputManipulations { get; } = [];
    }
}
