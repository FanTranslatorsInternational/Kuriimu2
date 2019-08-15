using Kompression.IO;

namespace Kompression.Specialized.SlimeMoriMori.Huffman
{
    class DefaultSlimeHuffmanReader : ISlimeHuffmanReader
    {
        public void BuildTree(BitReader br)
        {
            // We don't have a tree here
        }

        public byte ReadValue(BitReader br)
        {
            return br.ReadBits<byte>(8);
        }
    }
}
