namespace Kontract.Kompression
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
        /// <param name="literalRunLength">The current run of literals, including this one.</param>
        /// <param name="firstLiteralRun">If the given literal is part of the first literal run.</param>
        /// <returns>The calculated price.</returns>
        int CalculateLiteralPrice(int value,int literalRunLength,bool firstLiteralRun);

        /// <summary>
        /// Calculates the price of a pattern match.
        /// </summary>
        /// <param name="displacement">The displacement from the current position.</param>
        /// <param name="length">The length of the match.</param>
        /// <param name="matchRunLength">The current run of matches, including this one.</param>
        /// <returns>The calculated price.</returns>
        int CalculateMatchPrice(int displacement, int length, int matchRunLength);
    }
}
