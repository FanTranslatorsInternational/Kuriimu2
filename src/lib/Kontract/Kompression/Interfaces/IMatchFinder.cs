using System;
using Kontract.Kompression.Models;
using Kontract.Kompression.Models.PatternMatch;

namespace Kontract.Kompression.Interfaces
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
        /// Pre-processes the input for use in match finding operations.
        /// </summary>
        /// <param name="input">The input to preprocess.</param>
        void PreProcess(byte[] input);

        /// <summary>
        /// Finds matches at a certain position with the given limitations.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <param name="position">The position to search from.</param>
        /// <returns>All matches found at this position.</returns>
        AggregateMatch FindMatchesAtPosition(byte[] input, int position);
    }
}
