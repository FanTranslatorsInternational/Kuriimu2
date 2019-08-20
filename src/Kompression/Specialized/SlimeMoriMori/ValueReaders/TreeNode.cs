namespace Kompression.Specialized.SlimeMoriMori.ValueReaders
{
    class TreeNode
    {
        public TreeNode[] Children { get; }
        public int Value { get; set; } = -1;
        public bool IsLeaf => Value != -1;

        public TreeNode()
        {
            Children = new TreeNode[2];
        }
    }
}
