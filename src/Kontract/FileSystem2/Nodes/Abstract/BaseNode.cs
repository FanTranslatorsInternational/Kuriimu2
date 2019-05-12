//using System;
//using System.Diagnostics;

//namespace Kontract.FileSystem2.Nodes.Abstract
//{
//    /// <summary>
//    /// The base node.
//    /// </summary>
//    [DebuggerDisplay("{" + nameof(RelativePath) + "}")]
//    public abstract class BaseNode<T> : INode<T>
//    {
//        /// <summary>
//        /// Declares if the node is a directory.
//        /// </summary>
//        public abstract bool IsDirectory { get; }

//        /// <summary>
//        /// The name of ths node.
//        /// </summary>
//        public string Name { get; }

//        /// <summary>
//        /// The parent of this node.
//        /// </summary>
//        public T Parent { get; set; }

//        /// <summary>
//        /// The concatenated path of this node and its parent.
//        /// </summary>
//        public string RelativePath => BuildPath();

//        /// <summary>
//        /// Creates a new instance of <see cref="BaseNode{T}"/>.
//        /// </summary>
//        /// <param name="name">The name of this node.</param>
//        protected BaseNode(string name)
//        {
//            Name = name;
//        }


//        #region IDisposable Support

//        public bool Disposed { get; private set; }

//        protected virtual void Dispose(bool disposing)
//        {
//            if (Disposed)
//                return;

//            if (disposing)
//            {
//                Parent = null;
//            }

//            Disposed = true;
//        }

//        /// <inheritdoc cref="IDisposable.Dispose"/>
//        public void Dispose()
//        {
//            Dispose(true);
//        }

//        #endregion

//        private string BuildPath()
//        {
//            return !string.IsNullOrEmpty(Parent?.RelativePath) ?
//                $"{Parent?.RelativePath}{System.IO.Path.DirectorySeparatorChar}{Name}" :
//                Name;
//        }
//    }
//}
