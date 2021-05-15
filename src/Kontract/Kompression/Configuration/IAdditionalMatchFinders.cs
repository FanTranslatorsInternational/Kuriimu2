using System;
using Kontract.Kompression.Model;

namespace Kontract.Kompression.Configuration
{
    public interface IAdditionalMatchFinder : IInternalMatchOptions
    {
        /// <summary>
        /// Add another match finder.
        /// </summary>
        /// <param name="matchFinderFactory">The factory to create an <see cref="IMatchFinder"/>.</param>
        /// <returns>The option object.</returns>
        IMatchLimitations AndFindWith(Func<FindOptions, FindLimitations, IMatchFinder> matchFinderFactory);

        /// <summary>
        /// Adds a default match finder, which finds sequence patterns.
        /// </summary>
        /// <returns>The option object.</returns>
        IMatchLimitations AndFindMatches();

        /// <summary>
        /// Adds a default match finder, which finds repeating units.
        /// </summary>
        /// <returns>The option object.</returns>
        IMatchLimitations AndFindRunLength();

        /// <summary>
        /// Adds a default match finder, which finds repeating units of the given value.
        /// </summary>
        /// <param name="constant">The value to check for repetition.</param>
        /// <returns>The option object.</returns>
        IMatchLimitations AndFindConstantRunLength(int constant);
    }
}
