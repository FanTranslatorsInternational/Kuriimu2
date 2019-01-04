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

        IEnumerable<string> EnumerateFiles(bool relative = false);
        IEnumerable<string> EnumerateDirectories(bool relative = false);

        IVirtualFSRoot GetDirectory(string path);

        Stream OpenFile(string filename);
        Stream CreateFile(string filename);
        void DeleteFile(string filename);

        bool FileExists(string filename);
    }
}
