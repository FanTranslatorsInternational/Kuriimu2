using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.FileSystem2.Interfaces
{
    /// <summary>
    /// Base node interface.
    /// </summary>
    /// <typeparam name="TNodeImplementation">Extended implementation for parent use.</typeparam>
    public interface INode<TNodeImplementation> : IDisposable
    {
        /// <summary>
        /// Declares if the node is a directory.
        /// </summary>
        bool IsDirectory { get; }

        /// <summary>
        /// The name of ths node.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The parent of this node.
        /// </summary>
        TNodeImplementation Parent { get; set; }

        /// <summary>
        /// The concatenated path of this node and its parent.
        /// </summary>
        string RelativePath { get; }
    }
}
