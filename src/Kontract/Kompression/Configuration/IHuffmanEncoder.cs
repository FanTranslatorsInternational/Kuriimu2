using System.IO;

namespace Kontract.Kompression.Configuration
{
    /// <summary>
    /// Provides functionality to encode data.
    /// </summary>
    public interface IHuffmanEncoder
    {
        /// <summary>
        /// Encodes a stream of data.
        /// </summary>
        /// <param name="input">The input data to encode.</param>
        /// <param name="output">The output to encode to.</param>
        /// <param name="treeBuilder">The tree builder for this huffman compression.</param>
        void Encode(Stream input, Stream output, IHuffmanTreeBuilder treeBuilder);
    }
}
