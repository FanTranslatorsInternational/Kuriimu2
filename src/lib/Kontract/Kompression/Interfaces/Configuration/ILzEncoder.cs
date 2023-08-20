using System.Collections.Generic;
using System.IO;
using Kontract.Kompression.Models.PatternMatch;

namespace Kontract.Kompression.Interfaces.Configuration
{
    /// <summary>
    /// Provides functionality to encode data.
    /// </summary>
    public interface ILzEncoder
    {
        /// <summary>
        /// Configures the match options for this specification.
        /// </summary>
        /// <param name="matchOptions">The options to configure.</param>
        void Configure(IInternalMatchOptions matchOptions);

        /// <summary>
        /// Encodes a stream of data.
        /// </summary>
        /// <param name="input">The input data to encode.</param>
        /// <param name="output">The output to encode to.</param>
        /// <param name="matches">The matches for the Lempel-Ziv compression.</param>
        void Encode(Stream input, Stream output, IEnumerable<Match> matches);
    }
}
