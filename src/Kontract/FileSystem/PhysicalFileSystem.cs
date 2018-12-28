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
        private List<FileStream> _openedFiles;

        public string RootDir { get; private set; }

        public bool CanCreateDirectories => CheckWritePermission();

        public bool CanCreateFiles => CheckWritePermission();

        public bool CanDeleteDirectories => CheckWritePermission();

        public bool CanDeleteFiles => CheckWritePermission();

        public PhysicalFileSystem(string root)
        {
            RootDir = Path.GetFullPath(root);

            _openedFiles = new List<FileStream>();
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

        public Stream OpenFile(string filename/*, FileMode mode*/)
        {
            var openedFile = File.Open(Path.Combine(RootDir, filename), FileMode.Open);

            CleanOpenedFiles();
            _openedFiles.Add(openedFile);

            return openedFile;
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

        private void CleanOpenedFiles()
        {
            List<int> toDelete = new List<int>();
            for (int i = 0; i < _openedFiles.Count; i++)
                if (!_openedFiles[i].CanRead)
                    toDelete.Add(i);

            foreach (var toClose in toDelete.OrderByDescending(x => x))
            {
                _openedFiles[toClose].Close();
                _openedFiles.RemoveAt(toClose);
            }
        }

        public void Dispose()
        {
            foreach (var openFile in _openedFiles)
                openFile.Close();
        }
    }
}
