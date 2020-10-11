using System.Collections.Generic;
using System.IO;
using Kontract.Kompression.Model.PatternMatch;

namespace Kontract.Kompression.Configuration
{
    /// <summary>
    /// Provides functionality to encode data.
    /// </summary>
    public interface ILzHuffmanEncoder
    {
        /// <summary>
        /// Encodes a stream of data.
        /// </summary>
        /// <param name="input">The input data to encode.</param>
        /// <param name="output">The output to encode to.</param>
        /// <param name="matches">The matches for the Lempel-Ziv compression.</param>
        /// <param name="treeBuilder">The tree builder for this huffman compression.</param>
        void Encode(Stream input, Stream output, IEnumerable<Match> matches, IHuffmanTreeBuilder treeBuilder);
    }
}
