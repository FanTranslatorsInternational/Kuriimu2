using Kontract.Interfaces.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.Interfaces.Common
{
    public interface IMultipleFiles
    {
        IFileSystem FileSystem { get; set; }
    }
}
