using Komponent.IO;
using Kompression.DataClasses.Decoder.TaikoLz81;

namespace Kompression.Decoder.TaikoLz81
{
    internal class TaikoLz81Tree
    {
        private TaikoLz81Node? _root;

        public void Build(BinaryBitReader br, int valueBitCount)
        {
            _root = new TaikoLz81Node();

            ReadNode(br, _root, valueBitCount);
        }

        public int ReadValue(BinaryBitReader br)
        {
            if (_root == null)
                throw new InvalidOperationException("Tree has to be built first.");

            TaikoLz81Node node = _root;
            while (!node.IsLeaf)
                node = node.Children[br.ReadBit()];
            return node.Value;
        }

        private static void ReadNode(BinaryBitReader br, TaikoLz81Node node, int valueBitCount)
        {
            var flag = br.ReadBit();
            if (flag != 0)
            {
                node.Children[0] = new TaikoLz81Node();
                ReadNode(br, node.Children[0], valueBitCount);

                node.Children[1] = new TaikoLz81Node();
                ReadNode(br, node.Children[1], valueBitCount);
            }
            else
            {
                node.Value = br.ReadBits<int>(valueBitCount);
            }
        }
    }
}
