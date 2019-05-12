//using System.IO;

//namespace Kontract.FileSystem2.Nodes.Abstract
//{
//    /// <summary>
//    /// The base file node.
//    /// </summary>
//    public abstract class BaseFileNode<T> : BaseNode<T> where T : BaseNode<T>
//    {
//        /// <inheritdoc cref="BaseNode{T}.IsDirectory"/>
//        public override bool IsDirectory => false;

//        /// <summary>
//        /// Creates a new instance of <see cref="BaseFileNode{T}"/>.
//        /// </summary>
//        /// <param name="name">The name of this node.</param>
//        protected BaseFileNode(string name) : base(name)
//        {
//        }

//        /// <summary>
//        /// Open the file of this node.
//        /// </summary>
//        /// <returns><see cref="Stream"/> of this file node.</returns>
//        public abstract Stream Open();
//    }
//}
