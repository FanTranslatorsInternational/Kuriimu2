namespace Kontract.Kompression.Interfaces.Configuration
{
    public interface IMatchLimitations : IInternalMatchOptions
    {
	    /// <summary>
        /// Sets an additional boundary to ind matches in.
        /// </summary>
        /// <param name="minLength">The minimum length a pattern must have.</param>
        /// <param name="maxLength">The maximum length a pattern must have.</param>
        /// <returns>The option object.</returns>
        IAdditionalMatchFinder WithinLimitations(int minLength, int maxLength);

        /// <summary>
        /// Sets an additional boundary to ind matches in.
        /// </summary>
        /// <param name="minLength">The minimum length a pattern must have.</param>
        /// <param name="maxLength">The maximum length a pattern must have.</param>
        /// <param name="minDisplacement">The minimum displacement a pattern must be found at.</param>
        /// <param name="maxDisplacement">The maximum displacement a pattern must be found at.</param>
        /// <returns>The option object.</returns>
        IAdditionalMatchFinder WithinLimitations(int minLength, int maxLength, int minDisplacement, int maxDisplacement);
    }
}
