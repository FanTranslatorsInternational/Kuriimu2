using System;

namespace Kontract.FileSystem2.Nodes.Abstract
{
    /// <summary>
    /// The base node.
    /// </summary>
    public abstract class BaseNode : IDisposable
    {
        /// <summary>
        /// Declares if the node is a directory.
        /// </summary>
        public abstract bool IsDirectory { get; }

        /// <summary>
        /// The name of ths node.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The concatinated path of this node and its parent.
        /// </summary>
        public string Path => $"{Parent?.Path}{System.IO.Path.DirectorySeparatorChar}{Name}";

        /// <summary>
        /// The parent of this node.
        /// </summary>
        public BaseNode Parent { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="BaseNode"/>.
        /// </summary>
        /// <param name="name">The name of this node.</param>
        protected BaseNode(string name)
        {
            Name = name;
        }

        #region IDisposable Support

        public bool Disposed { get; private set; }

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed)
                return;

            if (disposing)
            {
                Parent.Dispose();
                Parent = null;
            }

            Disposed = true;
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
