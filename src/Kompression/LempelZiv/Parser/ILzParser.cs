using System;

namespace Kompression.LempelZiv.Parser
{
    /// <summary>
    /// A parser for collecting the best possible pattern matches.
    /// </summary>
    public interface ILzParser : IDisposable
    {
        /// <summary>
        /// Parses the input data for pattern matches.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <param name="startPosition">The position to start at in the input data.</param>
        /// <returns></returns>
        LzMatch[] Parse(byte[] input, int startPosition);
    }
}
