namespace Kontract.Kompression.Interfaces.Configuration
{
    public interface IInternalHuffmanOptions : IHuffmanOptions
    {
        /// <summary>
        /// Builds the huffman tree from the set options.
        /// </summary>
        /// <returns>The newly created huffman tree.</returns>
        IHuffmanTreeBuilder BuildHuffmanTree();
    }
}
