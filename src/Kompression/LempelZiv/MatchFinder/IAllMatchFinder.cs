using System;
using System.Collections.Generic;

namespace Kompression.LempelZiv.MatchFinder
{
    /// <summary>
    /// A finder for all pattern matches.
    /// </summary>
    public interface IAllMatchFinder : IMatchFinder, IDisposable
    {
        /// <summary>
        /// Finds all possible pattern matches at a given position.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <param name="position">The position to search the matches at.</param>
        /// <param name="limit">An optional limit as to how many matches can be found.</param>
        /// <returns>All matches.</returns>
        IEnumerable<LzMatch> FindAllMatches(byte[] input, int position, int limit = -1);
    }
}
