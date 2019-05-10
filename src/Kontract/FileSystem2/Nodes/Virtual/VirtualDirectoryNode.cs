using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontract.FileSystem2.Nodes.Abstract;

namespace Kontract.FileSystem2.Nodes.Virtual
{
    internal sealed class VirtualDirectoryNode : BaseDirectoryNode
    {
        public VirtualDirectoryNode(string name) : base(name)
        {
        }
    }
}
