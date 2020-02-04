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
        /// <param name="state">The current match state.</param>
        /// <param name="position">The current position.</param>
        /// <param name="value">The value to price.</param>
        /// <returns>The calculated price.</returns>
        int CalculateLiteralPrice(IMatchState state, int position, int value);

        /// <summary>
        /// Calculates the price of a pattern match.
        /// </summary>
        /// <param name="state">The current match state.</param>
        /// <param name="position">The current position.</param>
        /// <param name="displacement">The displacement from the current position.</param>
        /// <param name="length">The length of the match.</param>
        /// <returns>The calculated price.</returns>
        int CalculateMatchPrice(IMatchState state, int position, int displacement, int length);
    }

    /// <summary>
    /// Provides functionality to calculate prices for literals and pattern matches.
    /// </summary>
    public interface IPriceCalculator2
    {
        /// <summary>
        /// Calculates the price of a literal.
        /// </summary>
        /// <param name="state">The current match state.</param>
        /// <param name="position">The current position.</param>
        /// <param name="value">The value to price.</param>
        /// <returns>The calculated price.</returns>
        int CalculateLiteralPrice(int position, int value,int runLength, bool matchRun);

        /// <summary>
        /// Calculates the price of a pattern match.
        /// </summary>
        /// <param name="state">The current match state.</param>
        /// <param name="position">The current position.</param>
        /// <param name="displacement">The displacement from the current position.</param>
        /// <param name="length">The length of the match.</param>
        /// <returns>The calculated price.</returns>
        int CalculateMatchPrice(int position, int displacement, int length, int runLength, bool matchRun);
    }
}
