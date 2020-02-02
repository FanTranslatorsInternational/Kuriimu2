using System;
using Kontract.Kompression.Model;

namespace Kontract.Kompression.Configuration
{
    public interface IMatchAdditionalFinders : IMatchOptions
    {
        /// <summary>
        /// Sets the usage of another default match finder and allows setting its limitations.
        /// </summary>
        IMatchLimitations AndWithDefaultMatchFinder { get; }

        /// <summary>
        /// Sets the factory to add an additional match finder to the already existing ones.
        /// </summary>
        /// <param name="matchFinderFactory">The factory to add another <see cref="IMatchFinder"/>.</param>
        /// <returns>The option object.</returns>
        IMatchLimitations AndWith(Func<FindLimitations, FindOptions, IMatchFinder> matchFinderFactory);
    }
}
