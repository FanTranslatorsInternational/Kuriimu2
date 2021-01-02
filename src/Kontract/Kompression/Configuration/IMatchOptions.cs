using System;
using Kontract.Kompression.Model;

namespace Kontract.Kompression.Configuration
{
    /// <summary>
    /// Provides functionality to the configure pattern match operations.
    /// </summary>
    public interface IMatchOptions
    {
        /// <summary>
        /// Sets the factory to create an instance of <see cref="IMatchParser"/>.
        /// </summary>
        /// <param name="matchParserFactory">The factory to create an instance of <see cref="IMatchParser"/>.</param>
        /// <returns>The option object.</returns>
        IMatchOptions ParseMatchesWith(Func<FindOptions, IPriceCalculator, IMatchFinder[], IMatchParser> matchParserFactory);

        /// <summary>
        /// Sets the number of tasks to use to find pattern matches.
        /// </summary>
        /// <param name="count">The number of tasks.</param>
        /// <returns>The option objects.</returns>
        IMatchOptions ProcessWithTasks(int count);
    }
}
