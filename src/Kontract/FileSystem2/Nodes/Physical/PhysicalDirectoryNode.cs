using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kontract.FileSystem2.Nodes.Abstract;

namespace Kontract.FileSystem2.Nodes.Physical
{
    internal sealed class PhysicalDirectoryNode : BaseDirectoryNode
    {
        public PhysicalDirectoryNode(string name) : base(name)
        {
        }
    }
}
