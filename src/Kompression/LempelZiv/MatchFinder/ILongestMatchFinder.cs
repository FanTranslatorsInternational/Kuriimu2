using System;

namespace Kompression.LempelZiv.MatchFinder
{
    /// <summary>
    /// A finder for the longest pattern matches.
    /// </summary>
    public interface ILongestMatchFinder : IMatchFinder, IDisposable
    {
        /// <summary>
        /// Finds the longest match at a given position.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <param name="position">The position to search the match at.</param>
        /// <returns>The longest match.</returns>
        LzMatch FindLongestMatch(byte[] input, int position);
    }
}
