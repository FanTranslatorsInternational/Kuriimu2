using System;
using System.Collections.Generic;
using System.IO;
using Kompression.Configuration;
using Kompression.Models;

namespace Kompression.Interfaces
{
    /// <summary>
    /// Provides functionality to parse pattern matches.
    /// </summary>
    public interface IMatchParser : IDisposable
    {
        /// <summary>
        /// Gets all configured options to find matches with.
        /// </summary>
        FindOptions FindOptions { get; }

        /// <summary>
        /// Parses the found matches.
        /// </summary>
        /// <param name="input">The input data to parse matches from.</param>
        /// <returns>All parsed matches.</returns>
        IEnumerable<Match> ParseMatches(Stream input);
    }
}
