using System;
using System.Diagnostics;

namespace Kontract.FileSystem2.Nodes.Abstract
{
    /// <summary>
    /// The base node.
    /// </summary>
    [DebuggerDisplay("{Path}")]
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
        public virtual string Path => BuildPath();

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

        private string BuildPath()
        {
            var result = Parent?.Path ?? string.Empty;
            if (!string.IsNullOrEmpty(Name))
                result += $"{System.IO.Path.DirectorySeparatorChar}{Name}";
            return result;
        }

        #region IDisposable Support

        public bool Disposed { get; private set; }

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed)
                return;

            if (disposing)
            {
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
