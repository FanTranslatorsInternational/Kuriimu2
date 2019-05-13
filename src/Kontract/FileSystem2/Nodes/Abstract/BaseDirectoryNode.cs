using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.FileSystem2.Nodes.Abstract
{
    public abstract class BaseDirectoryNode<TDirectory,TFile>:BaseReadOnlyDirectoryNode
    {
        public BaseDirectoryNode(string name) : base(name)
        {
        }

        #region Add

        /// <summary>
        /// Add a dirctory to the tree.
        /// </summary>
        /// <param name="directory">Directory to add.</param>
        public abstract void AddDirectory(TDirectory directory);

        /// <summary>
        /// Add a file to the tree.
        /// </summary>
        /// <param name="file">File to add.</param>
        public abstract void AddFile(TFile file);

        #endregion

        #region Remove nodes

        /// <summary>
        /// Removes a directory.
        /// </summary>
        /// <param name="directory">Directory to remove.</param>
        /// <returns>Was directory removed.</returns>
        public abstract bool RemoveDirectory(TDirectory directory);

        /// <summary>
        /// Removes a file.
        /// </summary>
        /// <param name="file">File to remove.</param>
        /// <returns>Was file removed.</returns>
        public abstract bool RemoveFile(TFile file);

        #endregion
    }
}
