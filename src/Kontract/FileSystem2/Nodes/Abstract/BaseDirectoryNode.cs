using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kontract.Exceptions.FileSystem;

namespace Kontract.FileSystem2.Nodes.Abstract
{
    /// <summary>
    /// The base directory node.
    /// </summary>
    public abstract class BaseDirectoryNode : BaseNode
    {
        /// <inheritdoc cref="BaseNode.IsDirectory"/>
        public override bool IsDirectory => true;

        /// <summary>
        /// The children nodes this directory node holds.
        /// </summary>
        public IList<BaseNode> Children { get; }

        /// <summary>
        /// Creates a new instance of <see cref="BaseDirectoryNode"/>.
        /// </summary>
        /// <param name="name">The name of this node.</param>
        protected BaseDirectoryNode(string name) : base(name)
        {
            Children = new List<BaseNode>();
        }

        #region Containment

        /// <summary>
        /// Decides if a directory is contained down the node tree.
        /// </summary>
        /// <param name="directory">Directory to search down the node tree.</param>
        /// <returns>Is directory contained.</returns>
        public bool ContainsDirectory(string directory)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(BaseDirectoryNode));

            try
            {
                GetDirectoryNode(directory);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Decides if a file is contained down the node tree.
        /// </summary>
        /// <param name="filePath">File to search down the node tree.</param>
        /// <returns>Is file contained.</returns>
        public bool ContainsFile(string filePath)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(BaseDirectoryNode));

            try
            {
                GetFileNode(filePath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Enumeration

        /// <summary>
        /// Enumerate all directory nodes in this node.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<BaseDirectoryNode> EnumerateDirectories()
        {
            if (Disposed) throw new ObjectDisposedException(nameof(BaseDirectoryNode));

            // ReSharper disable once SuspiciousTypeConversion.Global
            return Children.Where(x => x.IsDirectory).Cast<BaseDirectoryNode>();
        }

        /// <summary>
        /// Enumerate all file nodes in this node.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<BaseFileNode> EnumerateFiles()
        {
            if (Disposed) throw new ObjectDisposedException(nameof(BaseDirectoryNode));

            // ReSharper disable once SuspiciousTypeConversion.Global
            return Children.Where(x => !x.IsDirectory).Cast<BaseFileNode>();
        }

        #endregion

        #region Get nodes

        /// <summary>
        /// Gets a directory node with the given path relative to this node.
        /// </summary>
        /// <param name="relativePath">Relative path.</param>
        /// <returns>Found directory node.</returns>
        public BaseDirectoryNode GetDirectoryNode(string relativePath)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(BaseDirectoryNode));
            if (string.IsNullOrEmpty(relativePath)) throw new ArgumentException(nameof(relativePath));

            var unifiedPath = Common.UnifyPath(relativePath);
            var split = unifiedPath.Split(System.IO.Path.DirectorySeparatorChar);
            if (split.Length > 1)
            {
                var matchingDir = GetDirectoryNode(split[0]);
                if (matchingDir == null)
                    throw new DirectoryNotFoundException($"{Path}{System.IO.Path.DirectorySeparatorChar}{split[0]}");

                // Iterate down to retrieve the directory
                return matchingDir.GetDirectoryNode(string.Join(System.IO.Path.DirectorySeparatorChar.ToString(), split.Skip(1).ToArray()));
            }
            else
            {
                var matchingDir = Children.Where(x => x.IsDirectory).FirstOrDefault(x => x.Name == split[0]);
                if (matchingDir == null)
                    throw new DirectoryNotFoundException($"{Path}{System.IO.Path.DirectorySeparatorChar}{split[0]}");

                return (BaseDirectoryNode)matchingDir;
            }
        }

        /// <summary>
        /// Gets a file node with the given path relative to this node.
        /// </summary>
        /// <param name="relativePath">Relative path.</param>
        /// <returns>Found file node.</returns>
        public BaseFileNode GetFileNode(string relativePath)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(BaseDirectoryNode));
            if (string.IsNullOrEmpty(relativePath)) throw new ArgumentException(nameof(relativePath));

            var unifiedPath = Common.UnifyPath(relativePath);
            var split = unifiedPath.Split(System.IO.Path.DirectorySeparatorChar);
            if (split.Length > 1)
            {
                var matchingDir = GetDirectoryNode(split[0]);
                if (matchingDir == null)
                    throw new DirectoryNotFoundException($"{Path}{System.IO.Path.DirectorySeparatorChar}{split[0]}");

                // Iterate down to retrieve the directory
                return matchingDir.GetFileNode(string.Join(System.IO.Path.DirectorySeparatorChar.ToString(), split.Skip(1).ToArray()));
            }

            var matchingFile = Children.Where(x => !x.IsDirectory).FirstOrDefault(x => x.Name == split[0]);
            if (matchingFile == null)
                throw new FileNotFoundException($"{Path}{System.IO.Path.DirectorySeparatorChar}{split[0]}");

            return (BaseFileNode)matchingFile;
        }

        #endregion

        #region Add

        /// <summary>
        /// Add any <see cref="BaseNode"/> to the children nodes.
        /// </summary>
        /// <param name="node">Node to add.</param>
        public void Add(BaseNode node)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(BaseDirectoryNode));
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (ContainsFile(node.Name))
                throw new NodeFoundException(node);
            if (ContainsDirectory(node.Name))
            {
                var dirNode = GetDirectoryNode(node.Name);
                foreach (var child in (node as BaseDirectoryNode).Children)
                {
                    dirNode.Add(child);
                }
            }
            else
            {
                Children.Add(node);
            }

            node.Parent = this;
        }

        /// <summary>
        /// Add a collection of nodes to the children nodes.
        /// </summary>
        /// <param name="nodes">Nodes to add.</param>
        public void AddRange(IEnumerable<BaseNode> nodes)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(BaseDirectoryNode));
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));

            foreach (var node in nodes)
                Add(node);
        }

        #endregion

        #region Remove

        /// <summary>
        /// Remove node from children nodes.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool Remove(BaseNode node)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(BaseDirectoryNode));
            if (node == null) throw new ArgumentNullException(nameof(node));

            return Children.Remove(node);
        }

        /// <summary>
        /// Clear all children nodes.
        /// </summary>
        public void ClearChildren()
        {
            if (Disposed) throw new ObjectDisposedException(nameof(BaseDirectoryNode));

            Children.Clear();
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (Disposed)
                return;

            if (disposing)
            {
                foreach (var child in Children)
                    child.Dispose();
                ClearChildren();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
