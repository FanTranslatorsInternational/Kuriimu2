using System;
using System.Collections.Generic;
using Kontract.Kompression.Model;
using Kontract.Kompression.Model.PatternMatch;

namespace Kontract.Kompression
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
        /// <param name="startPosition">Position from which to pre process the input.</param>
        void PreProcess(byte[] input, int startPosition);

        /// <summary>
        /// Finds matches at a certain position with the given limitations.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <param name="position">The position to search from.</param>
        /// <returns>All matches found at this position.</returns>
        IList<Match> FindMatchesAtPosition(byte[] input, int position);
    }
}
