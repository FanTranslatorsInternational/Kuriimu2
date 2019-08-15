using Kompression.IO;

namespace Kompression.Specialized.SlimeMoriMori.Huffman
{
    class SlimeHuffman8Reader : ISlimeHuffmanReader
    {
        private HuffmanTree _tree;

        public void BuildTree(BitReader br)
        {
            _tree = new HuffmanTree(8);
            _tree.Build(br);
        }

        public byte ReadValue(BitReader br)
        {
            return _tree.GetValue(br);
        }
    }
}
