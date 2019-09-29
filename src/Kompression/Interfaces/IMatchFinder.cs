using System;
using System.Collections.Generic;
using Kompression.Configuration;
using Kompression.Models;

namespace Kompression.Interfaces
{
    /// <summary>
    /// Provides functionality for finding matches.
    /// </summary>
    public interface IMatchFinder : IDisposable
    {
        /// <summary>
        /// Gets the limitations to find matches in.
        /// </summary>
        FindLimitations FindLimitations { get; }

        /// <summary>
        /// Gets all configured options to find matches with.
        /// </summary>
        FindOptions FindOptions { get; }

        /// <summary>
        /// Finds matches at a certain position with the given limitations.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <param name="position">The position to search from.</param>
        /// <returns>All matches found at this position.</returns>
        IEnumerable<Match> FindMatchesAtPosition(byte[] input, int position);

        /// <summary>
        /// Finds matches at all positions with the given limitations.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <param name="position">The position to start search at.</param>
        /// <returns>All matches found in the input data.</returns>
        IEnumerable<Match> GetAllMatches(byte[] input, int position);
    }
}
