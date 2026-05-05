using Komponent.IO;
using Kompression.DataClasses.SlimeMoriMori.ValueReader;
using Kompression.InternalContract.SlimeMoriMori.ValueReader;

namespace Kompression.Specialized.SlimeMoriMori.ValueReader
{
    internal class HuffmanReader(int bitDepth) : IValueReader
    {
        protected TreeNode? Root { get; private set; }

        public void BuildTree(BinaryBitReader br)
        {
            Root = new TreeNode();

            // Explanation of code:
            // read all values with n bits continuously
            // put those values at their respective node; the tree gets traversed from left to right on the same bit depth

            var treePath = 0;
            for (var i = 0; i < 16; i++)
            {
                treePath <<= 1;
                var huffmanValueCount = br.ReadBits<int>(8);
                for (var j = 0; j < huffmanValueCount; j++)
                {
                    // Traverse tree to hit value node
                    var node = Root;

                    for (var h = i; h > 0; h--)
                    {
                        var childIndex = (treePath >> h) & 0x1;
                        node!.Children[childIndex] ??= new TreeNode();

                        node = node.Children[childIndex];
                    }

                    // Set value in tree
                    var value = br.ReadBits<int>(bitDepth);
                    node!.Children[treePath & 0x1] = new TreeNode
                    {
                        Value = value
                    };
                    //_table[(treePath & 0x1) * 2 + tableIndex] = (byte)~value;
                    //_table[(treePath & 0x1) * 2 + tableIndex + 1] = (byte)(~value >> 8);

                    treePath++;
                }
            }
        }

        public byte ReadValue(BinaryBitReader br)
        {
            if (Root == null)
                throw new InvalidOperationException("Did not build tree.");

            var node = Root;

            while (!node!.IsLeaf)
                node = node.Children[br.ReadBit()];

            return (byte)node.Value;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Root = null;
            }
        }
    }
}
