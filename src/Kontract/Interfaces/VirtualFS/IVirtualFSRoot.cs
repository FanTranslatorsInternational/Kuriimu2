using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.Interfaces.VirtualFS
{
    public interface IVirtualFSRoot : IDisposable
    {
        bool CanCreateDirectories { get; }
        bool CanCreateFiles { get; }
        bool CanDeleteDirectories { get; }
        bool CanDeleteFiles { get; }

        string RootDir { get; }

        IEnumerable<string> EnumerateFiles();
        IEnumerable<string> EnumerateDirectories();

        IVirtualFSRoot GetDirectory(string path);
        Stream OpenFile(string filename/*, FileMode mode*/);
    }
}
