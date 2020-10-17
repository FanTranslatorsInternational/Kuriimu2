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
    }
}
