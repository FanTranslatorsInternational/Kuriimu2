using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.FileSystem2.Interfaces
{
    public interface IPhysical : INode<IPhysical>
    {
        string Root { get; }
    }
}
