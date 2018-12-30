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

            if (!Directory.Exists(RootDir))
                Directory.CreateDirectory(RootDir);

            _openedFiles = new List<FileStream>();
        }

        public IEnumerable<string> EnumerateDirectories(bool relative = false)
        {
            if (!relative)
                return Directory.EnumerateDirectories(RootDir);

            return Directory.EnumerateDirectories(RootDir).Select(x => x.Remove(0, RootDir.Length + 1));
        }

        public IEnumerable<string> EnumerateFiles(bool relative = false)
        {
            if (!relative)
                return Directory.EnumerateFiles(RootDir);

            return Directory.EnumerateFiles(RootDir).Select(x => x.Remove(0, RootDir.Length + 1));
        }

        public IVirtualFSRoot GetDirectory(string path)
        {
            return new PhysicalFileSystem(Path.GetFullPath(Path.Combine(RootDir, path)));
        }

        public Stream OpenFile(string filename)
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
                _openedFiles[toClose].Dispose();
                _openedFiles.RemoveAt(toClose);
            }
        }

        public void Dispose()
        {
            foreach (var openFile in _openedFiles)
                openFile.Dispose();
        }

        public Stream CreateFile(string filename)
        {
            if (!CanCreateFiles)
                throw new InvalidOperationException("Can't create files");

            var createdFile = File.Create(Path.Combine(RootDir, filename));

            CleanOpenedFiles();
            _openedFiles.Add(createdFile);

            return createdFile;
        }
    }
}
