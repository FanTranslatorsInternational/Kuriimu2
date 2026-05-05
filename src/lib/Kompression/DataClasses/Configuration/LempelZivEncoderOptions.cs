using Kompression.Contract.Configuration;
using Kompression.Contract.DataClasses.Encoder.LempelZiv.MatchFinder;
using Kompression.Contract.Enums.Encoder.LempelZiv;

namespace Kompression.DataClasses.Configuration
{
    internal class LempelZivEncoderOptions
    {
        public IList<CreateMatchFinderDelegate> MatchFinderDelegates { get; } = [];
        public IList<LempelZivMatchLimitations> MatchLimitations { get; } = [];
        public CreatePriceCalculatorDelegate? CalculatePriceDelegate { get; set; }
        public AdjustInputDelegate? AdjustInputDelegate { get; set; }
        public int SkipUnits { get; set; }
        public UnitSize UnitSize { get; set; } = UnitSize.Byte;
    }
}
