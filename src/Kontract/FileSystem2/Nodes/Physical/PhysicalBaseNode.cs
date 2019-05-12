//using System;
//using Kontract.FileSystem2.Nodes.Abstract;

//namespace Kontract.FileSystem2.Nodes.Physical
//{
//    public abstract class PhysicalBaseNode : BaseNode<PhysicalBaseNode>
//    {
//        public string RootDir { get; protected set; }

//        protected PhysicalBaseNode(string name, string root) : base(name)
//        {
//            if (string.IsNullOrEmpty(root)) throw new ArgumentException(nameof(root));
//            RootDir = root;
//        }
//    }
//}
