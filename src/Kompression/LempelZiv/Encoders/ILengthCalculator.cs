using Kompression.LempelZiv.Models;

namespace Kompression.LempelZiv.Encoders
{
    public interface ILengthCalculator
    {
        int CalculateLiteralLength(byte value);
        int CalculateMatchLength(LzMatch match);
    }
}
