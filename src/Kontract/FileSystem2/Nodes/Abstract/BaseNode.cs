using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Kontract.FileSystem2.Nodes.Abstract
{
    /// <summary>
    /// The base node.
    /// </summary>
    [DebuggerDisplay("{" + nameof(RelativePath) + "}")]
    public abstract class BaseNode
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
        /// The parent of this node.
        /// </summary>
        public BaseReadOnlyDirectoryNode Parent { get; set; }

        /// <summary>
        /// The concatenated path of this node and its parent.
        /// </summary>
        public string RelativePath => BuildPath(Name);

        /// <summary>
        /// Creates a new instance of <see cref="BaseNode"/>.
        /// </summary>
        /// <param name="name">The name of this node.</param>
        protected BaseNode(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
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

        protected void CheckDisposed()
        {
            if (Disposed) throw new ObjectDisposedException(nameof(BaseReadOnlyDirectoryNode));
        }

        #endregion

        protected string BuildPath(string lastElement)
        {
            return !string.IsNullOrEmpty(Parent?.RelativePath) ?
                $"{Parent?.RelativePath}{System.IO.Path.DirectorySeparatorChar}{lastElement}" :
                lastElement;
        }

        protected string ResolvePath(string path)
        {
            var unified = Common.UnifyPath(path);
            var resolvedPath = new List<string>();
            foreach (var s in unified.Split(Path.DirectorySeparatorChar))
            {
                if (s == ".")
                    continue;
                if (s == ".." && resolvedPath.Any())
                {
                    resolvedPath.RemoveAt(resolvedPath.Count - 1);
                    continue;
                }

                resolvedPath.Add(s);
            }

            return Common.UnifyPath(Path.Combine(resolvedPath.ToArray()));
        }
    }
}
