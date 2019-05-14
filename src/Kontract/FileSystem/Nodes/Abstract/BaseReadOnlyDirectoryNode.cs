using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Kontract.FileSystem.Exceptions;

namespace Kontract.FileSystem.Nodes.Abstract
{
    /// <summary>
    /// The base directory node.
    /// </summary>
    public abstract class BaseReadOnlyDirectoryNode : BaseNode
    {
        protected abstract IEnumerable<BaseNode> ProtectedChildren { get; }

        /// <inheritdoc cref="BaseNode.IsDirectory"/>
        public override bool IsDirectory => true;

        /// <summary>
        /// The children nodes this directory holds.
        /// </summary>
        public IReadOnlyList<BaseNode> Children =>
            new ReadOnlyCollection<BaseNode>(ProtectedChildren.ToList());

        /// <summary>
        /// Creates a new instance of <see cref="BaseReadOnlyDirectoryNode"/>.
        /// </summary>
        /// <param name="name">The name of this node.</param>
        protected BaseReadOnlyDirectoryNode(string name) : base(name)
        {
        }

        #region Containment

        /// <summary>
        /// Decides if a directory is contained down the node tree.
        /// </summary>
        /// <param name="directory">Directory to search down the node tree.</param>
        /// <returns>Is directory contained.</returns>
        public bool ContainsDirectory(string directory)
        {
            CheckDisposed();

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
            CheckDisposed();

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
        public IEnumerable<BaseReadOnlyDirectoryNode> EnumerateDirectories()
        {
            CheckDisposed();

            return Children.Where(x => x is BaseReadOnlyDirectoryNode).Cast<BaseReadOnlyDirectoryNode>();
        }

        /// <summary>
        /// Enumerate all file nodes in this node.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<BaseFileNode> EnumerateFiles()
        {
            CheckDisposed();

            return Children.Where(x => x is BaseFileNode).Cast<BaseFileNode>();
        }

        #endregion

        #region Get nodes

        /// <summary>
        /// Gets a directory node with the given path relative to this node.
        /// </summary>
        /// <param name="relativePath">Relative path.</param>
        /// <returns>Found directory node.</returns>
        public BaseReadOnlyDirectoryNode GetDirectoryNode(string relativePath)
        {
            BaseReadOnlyDirectoryNode Matcher(string name)
            {
                var matchingFile = EnumerateDirectories().FirstOrDefault(x => x.Name == name);
                return matchingFile ?? throw new DirectoryNotFoundException(RelativePath);
            }

            return GetNode(relativePath,
                (matchingDir, newRelativePath) => matchingDir.GetDirectoryNode(newRelativePath),
                Matcher);
        }

        /// <summary>
        /// Gets a file node with the given path relative to this node.
        /// </summary>
        /// <param name="relativePath">Relative path.</param>
        /// <returns>Found file node.</returns>
        public BaseFileNode GetFileNode(string relativePath)
        {
            BaseFileNode Matcher(string name)
            {
                var matchingFile = EnumerateFiles().FirstOrDefault(x => x.Name == name);
                return matchingFile ?? throw new FileNotFoundException($"{RelativePath}{Path.DirectorySeparatorChar}{name}");
            }

            return GetNode(relativePath,
                (matchingDir, newRelativePath) => matchingDir.GetFileNode(newRelativePath),
                Matcher);
        }

        private TNode GetNode<TNode>(string relativePath, Func<BaseReadOnlyDirectoryNode, string, TNode> iterator, Func<string, TNode> matcher)
        {
            CheckDisposed();
            if (string.IsNullOrEmpty(relativePath)) throw new ArgumentException(nameof(relativePath));

            var resolvedPath = ResolvePath(relativePath);
            var split = resolvedPath.Split(Path.DirectorySeparatorChar);
            if (split[0] == "..")
                if (Parent == null)
                    throw new PathOutOfRangeException(relativePath);
                else
                    return iterator(Parent, string.Join(Path.DirectorySeparatorChar.ToString(), split.Skip(1)));

            if (split.Length <= 1)
                return matcher(split[0]);

            var matchingDir = GetDirectoryNode(split[0]);
            if (matchingDir == null)
                throw new DirectoryNotFoundException(BuildPath(split[0]));

            // Iterate down to retrieve the directory
            return iterator(matchingDir,
                string.Join(Path.DirectorySeparatorChar.ToString(), split.Skip(1).ToArray()));

        }

        #endregion
    }
}
