using Kontract.Interfaces.VirtualFS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.FileSystem
{
    public class PhysicalFileSystem : IVirtualFSRoot
    {
        public string RootDir { get; private set; }

        public bool CanCreateDirectories => CheckWritePermission();

        public bool CanCreateFiles => CheckWritePermission();

        public bool CanDeleteDirectories => CheckWritePermission();

        public bool CanDeleteFiles => CheckWritePermission();

        public PhysicalFileSystem(string root)
        {
            RootDir = Path.GetFullPath(root);
        }

        public IEnumerable<string> EnumerateDirectories()
        {
            return Directory.EnumerateDirectories(RootDir);
        }

        public IEnumerable<string> EnumerateFiles()
        {
            return Directory.EnumerateFiles(RootDir);
        }

        public IVirtualFSRoot GetDirectory(string path)
        {
            return new PhysicalFileSystem(Path.GetFullPath(Path.Combine(RootDir, path)));
        }

        public FileStream OpenFile(string filename, FileMode mode)
        {
            return File.Open(Path.Combine(RootDir, filename), mode);
        }

        private bool CheckWritePermission()
        {
            try
            {
                var permissions = Directory.GetAccessControl(RootDir);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
