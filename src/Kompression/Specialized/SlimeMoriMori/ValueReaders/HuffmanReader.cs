using Kompression.IO;

namespace Kompression.Specialized.SlimeMoriMori.ValueReaders
{
    class HuffmanReader : IValueReader
    {
        private readonly int _bitDepth;
        protected TreeNode Root { get; private set; }

        public HuffmanReader(int bitDepth)
        {
            _bitDepth = bitDepth;
        }

        public void BuildTree(BitReader br)
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
                    //var tableIndex = 0;
                    for (var h = i; h > 0; h--)
                    {
                        //var newTableIndex = ((treePath >> h) & 0x1) * 2 + tableIndex;
                        //tableIndex = (short)(_table[newTableIndex] | (_table[newTableIndex + 1] << 8));

                        var childIndex = (treePath >> h) & 0x1;
                        if (node.Children[childIndex] == null)
                        {
                            node.Children[childIndex] = new TreeNode();
                            //tableIndex = tableIndex2 + 4;

                            //// Reference to another node
                            //_table[newTableIndex] = (byte)tableIndex;
                            //_table[newTableIndex + 1] = (byte)(tableIndex >> 8);
                            //tableIndex2 = tableIndex;
                        }
                        node = node.Children[childIndex];
                    }

                    // Set value in tree
                    var value = br.ReadBits<int>(_bitDepth);
                    node.Children[treePath & 0x1] = new TreeNode
                    {
                        Value = value
                    };
                    //_table[(treePath & 0x1) * 2 + tableIndex] = (byte)~value;
                    //_table[(treePath & 0x1) * 2 + tableIndex + 1] = (byte)(~value >> 8);

                    treePath++;
                }
            }
        }

        public byte ReadValue(BitReader br)
        {
            var node = Root;
            while (!node.IsLeaf)
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
