using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kompression.PatternMatch
{
    /// <summary>
    /// A finder for matches and its basic properties.
    /// </summary>
    public interface IMatchFinder : IDisposable
    {
        /// <summary>
        /// The minimum size a match must have.
        /// </summary>
        int MinMatchSize { get; }

        /// <summary>
        /// The maximum size a match must have.
        /// </summary>
        int MaxMatchSize { get; }

        /// <summary>
        /// The minimum displacement from a certain position to find a match at.
        /// </summary>
        int MinDisplacement { get; }

        /// <summary>
        /// The maximum displacement from a certain position to find a match at.
        /// </summary>
        /// <remarks>Also referred to as window size.</remarks>
        int MaxDisplacement { get; }

        /// <summary>
        /// Defines the minimum unit size.
        /// </summary>
        DataType DataType { get; }

        /// <summary>
        /// Toggles the usage of looking ahead of the current position.
        /// </summary>
        bool UseLookAhead { get; }

        /// <summary>
        /// Finds matches at a certain position with the given limitations.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <param name="position">The position to search from.</param>
        /// <returns></returns>
        IEnumerable<Match> FindMatches(byte[] input, int position);
    }
}
