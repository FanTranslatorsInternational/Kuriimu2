using Kompression.Models;

namespace Kompression.Interfaces
{
    /// <summary>
    /// Provides functionality to calculate prices for literals and pattern matches.
    /// </summary>
    public interface IPriceCalculator
    {
        /// <summary>
        /// Calculates the price of a literal.
        /// </summary>
        /// <param name="value">The value to price.</param>
        /// <returns>The calculated price.</returns>
        int CalculateLiteralPrice(int value);

        /// <summary>
        /// Calculates the price of a pattern match.
        /// </summary>
        /// <param name="match">The pattern match to price.</param>
        /// <returns>The calculated price.</returns>
        int CalculateMatchPrice(Match match);
    }
}
