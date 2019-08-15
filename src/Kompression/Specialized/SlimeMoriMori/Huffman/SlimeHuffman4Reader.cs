using Kompression.IO;

namespace Kompression.Specialized.SlimeMoriMori.Huffman
{
    class SlimeHuffman4Reader : ISlimeHuffmanReader
    {
        private HuffmanTree _tree;

        public void BuildTree(BitReader br)
        {
            _tree = new HuffmanTree(4);
            _tree.Build(br);
        }

        public byte ReadValue(BitReader br)
        {
            return (byte)((_tree.GetValue(br) << 4) | _tree.GetValue(br));
        }
    }
}
