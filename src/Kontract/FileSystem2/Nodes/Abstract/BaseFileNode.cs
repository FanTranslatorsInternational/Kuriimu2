using System.IO;
using System.Threading;
using Kontract.Exceptions.FileSystem;
using Kontract.FileSystem2.IO;

namespace Kontract.FileSystem2.Nodes.Abstract
{
    /// <summary>
    /// The base file node.
    /// </summary>
    public abstract class BaseFileNode : BaseNode
    {
        /// <inheritdoc cref="BaseNode.IsDirectory"/>
        public override bool IsDirectory => false;

        /// <summary>
        /// Creates a new instance of <see cref="BaseFileNode"/>.
        /// </summary>
        /// <param name="name">The name of this node.</param>
        protected BaseFileNode(string name) : base(name)
        {
        }

        /// <summary>
        /// Open the file of this node.
        /// </summary>
        /// <returns><see cref="Stream"/> of this file node.</returns>
        public abstract Stream Open();
    }
}
