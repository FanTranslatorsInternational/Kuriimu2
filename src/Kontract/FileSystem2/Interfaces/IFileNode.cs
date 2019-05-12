using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.FileSystem2.Interfaces
{
    public interface IFileNode<T> : INode<T>
    {
        Stream Open();
    }
}
