namespace Kompression.DataClasses.SlimeMoriMori.ValueReader
{
    internal class TreeNode
    {
        public TreeNode?[] Children { get; } = new TreeNode?[2];
        public int Value { get; set; } = -1;
        public bool IsLeaf => Value != -1;
    }
}
