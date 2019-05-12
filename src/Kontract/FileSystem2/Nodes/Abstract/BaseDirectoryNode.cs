//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using Kontract.Exceptions.FileSystem;

//namespace Kontract.FileSystem2.Nodes.Abstract
//{
//    /// <summary>
//    /// The base directory node.
//    /// </summary>
//    public abstract class BaseDirectoryNode<T> : BaseNode<T> where T : BaseNode<T>
//    {
//        /// <inheritdoc cref="BaseNode{T}.IsDirectory"/>
//        public override bool IsDirectory => true;

//        /// <summary>
//        /// The children nodes this directory node holds.
//        /// </summary>
//        public abstract IEnumerable<T> Children { get; }

//        /// <summary>
//        /// Creates a new instance of <see cref="BaseDirectoryNode{T}"/>.
//        /// </summary>
//        /// <param name="name">The name of this node.</param>
//        protected BaseDirectoryNode(string name) : base(name)
//        {
//            if (name == null) throw new ArgumentNullException(nameof(name));
//        }

//        #region Containment

//        /// <summary>
//        /// Decides if a directory is contained down the node tree.
//        /// </summary>
//        /// <param name="directory">Directory to search down the node tree.</param>
//        /// <returns>Is directory contained.</returns>
//        public bool ContainsDirectory(string directory)
//        {
//            if (Disposed) throw new ObjectDisposedException(nameof(BaseDirectoryNode<T>));

//            try
//            {
//                GetDirectoryNode(directory);
//                return true;
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        /// <summary>
//        /// Decides if a file is contained down the node tree.
//        /// </summary>
//        /// <param name="filePath">File to search down the node tree.</param>
//        /// <returns>Is file contained.</returns>
//        public bool ContainsFile(string filePath)
//        {
//            if (Disposed) throw new ObjectDisposedException(nameof(BaseDirectoryNode<T>));

//            try
//            {
//                GetFileNode(filePath);
//                return true;
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        #endregion

//        #region Enumeration

//        /// <summary>
//        /// Enumerate all directory nodes in this node.
//        /// </summary>
//        /// <returns></returns>
//        public IEnumerable<BaseDirectoryNode<T>> EnumerateDirectories()
//        {
//            if (Disposed) throw new ObjectDisposedException(nameof(BaseDirectoryNode<T>));

//            return Children.Where(x => x is BaseDirectoryNode<T>).Cast<BaseDirectoryNode<T>>();
//        }

//        /// <summary>
//        /// Enumerate all file nodes in this node.
//        /// </summary>
//        /// <returns></returns>
//        public IEnumerable<BaseFileNode<T>> EnumerateFiles()
//        {
//            if (Disposed) throw new ObjectDisposedException(nameof(BaseDirectoryNode<T>));

//            return Children.Where(x => x is BaseFileNode<T>).Cast<BaseFileNode<T>>();
//        }

//        #endregion

//        #region Get nodes

//        /// <summary>
//        /// Gets a directory node with the given path relative to this node.
//        /// </summary>
//        /// <param name="relativePath">Relative path.</param>
//        /// <returns>Found directory node.</returns>
//        public BaseDirectoryNode<T> GetDirectoryNode(string relativePath)
//        {
//            BaseDirectoryNode<T> Matcher(string name)
//            {
//                var matchingFile = EnumerateDirectories().FirstOrDefault(x => x.Name == name);
//                return matchingFile ?? throw new DirectoryNotFoundException($"{RelativePath}{Path.DirectorySeparatorChar}{name}");
//            }

//            return GetNode(relativePath,
//                (matchingDir, newRelativePath) => matchingDir.GetDirectoryNode(newRelativePath),
//                Matcher);
//        }

//        /// <summary>
//        /// Gets a file node with the given path relative to this node.
//        /// </summary>
//        /// <param name="relativePath">Relative path.</param>
//        /// <returns>Found file node.</returns>
//        public BaseFileNode<T> GetFileNode(string relativePath)
//        {
//            BaseFileNode<T> Matcher(string name)
//            {
//                var matchingFile = EnumerateFiles().FirstOrDefault(x => x.Name == name);
//                return matchingFile ?? throw new FileNotFoundException($"{RelativePath}{Path.DirectorySeparatorChar}{name}");
//            }

//            return GetNode(relativePath,
//                (matchingDir, newRelativePath) => matchingDir.GetFileNode(newRelativePath),
//                Matcher);
//        }

//        private TNode GetNode<TNode>(string relativePath, Func<BaseDirectoryNode<T>, string, TNode> iterator, Func<string, TNode> matcher)
//        {
//            if (Disposed) throw new ObjectDisposedException(nameof(BaseDirectoryNode<T>));
//            if (string.IsNullOrEmpty(relativePath)) throw new ArgumentException(nameof(relativePath));

//            var unifiedPath = Common.UnifyPath(relativePath);
//            var split = unifiedPath.Split(Path.DirectorySeparatorChar);
//            if (split.Length > 1)
//            {
//                var matchingDir = GetDirectoryNode(split[0]);
//                if (matchingDir == null)
//                    throw new DirectoryNotFoundException($"{RelativePath}{Path.DirectorySeparatorChar}{split[0]}");

//                // Iterate down to retrieve the directory
//                return iterator(matchingDir,
//                    string.Join(Path.DirectorySeparatorChar.ToString(), split.Skip(1).ToArray()));
//            }

//            return matcher(split[0]);
//        }

//        #endregion

//        #region Add nodes

//        /// <summary>
//        /// Add a collection of nodes to the children nodes.
//        /// </summary>
//        /// <param name="nodes">Nodes to add.</param>
//        public void AddRange(IEnumerable<T> nodes)
//        {
//            if (Disposed) throw new ObjectDisposedException(nameof(BaseDirectoryNode<T>));
//            if (nodes == null) throw new ArgumentNullException(nameof(nodes));

//            foreach (var node in nodes)
//                AddNode(node);
//        }

//        /// <summary>
//        /// Add any <see cref="BaseNode{T}"/> to the children nodes.
//        /// </summary>
//        /// <param name="node">Node to add.</param>
//        public abstract void AddNode(T node);
//        //{
//        //if (Disposed) throw new ObjectDisposedException(nameof(BaseDirectoryNode<T>));
//        //if (node == null) throw new ArgumentNullException(nameof(node));
//        //if (ContainsFile(node.Name))
//        //    throw new NodeFoundException<T>(node);
//        //if (ContainsDirectory(node.Name))
//        //{
//        //    var containingDirNode = GetDirectoryNode(node.Name);
//        //    var children = (node as BaseDirectoryNode<T>)?.Children;
//        //    if (children == null) throw new ArgumentNullException(nameof(children));
//        //    foreach (var child in children)
//        //    {
//        //        containingDirNode.Add(child);
//        //    }
//        //}
//        //else
//        //{
//        //    Children.Add(node);
//        //}

//        //SetParent(node);
//        // node.Parent = (T)this;
//        //}

//        #endregion

//        #region Remove nodes

//        /// <summary>
//        /// Remove node from children nodes.
//        /// </summary>
//        /// <param name="node"></param>
//        /// <returns></returns>
//        public abstract bool RemoveNode(T node);
//        //{
//        //    if (Disposed) throw new ObjectDisposedException(nameof(BaseDirectoryNode<T>));
//        //    if (node == null) throw new ArgumentNullException(nameof(node));

//        //    return Children.Remove(node);
//        //}

//        #endregion
//    }
//}
