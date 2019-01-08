using Kontract.Interfaces.FileSystem;
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
    public class PhysicalFileSystem : IFileSystem
    {
        private List<Stream> _openedFiles;

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

            _openedFiles = new List<Stream>();
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

        public IFileSystem GetDirectory(string path)
        {
            return new PhysicalFileSystem(Path.GetFullPath(Path.Combine(RootDir, path)));
        }

        public Stream OpenFile(string filename)
        {
            var openedFile = File.Open(Path.Combine(RootDir, filename), FileMode.Open);
            var fsFileStream = new FileSystemStream(openedFile);
            fsFileStream.CloseStream += FsFileStream_CloseStream;

            _openedFiles.Add(openedFile);

            return fsFileStream;
        }

        public Stream CreateFile(string filename)
        {
            if (!CanCreateFiles)
                throw new InvalidOperationException("Can't create files.");

            var createdFile = File.Create(Path.Combine(RootDir, filename));
            var fsFileStream = new FileSystemStream(createdFile);
            fsFileStream.CloseStream += FsFileStream_CloseStream;

            _openedFiles.Add(createdFile);

            return fsFileStream;
        }

        public void DeleteFile(string filename)
        {
            if (!CanDeleteFiles)
                throw new InvalidOperationException("Can't delete files.");

            File.Delete(Path.Combine(RootDir, filename));
        }

        public bool FileExists(string filename)
        {
            return File.Exists(Path.Combine(RootDir, filename));
        }

        public void Dispose()
        {
            foreach (var openFile in _openedFiles)
                openFile.Dispose();
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

        private void FsFileStream_CloseStream(object sender, CloseStreamEventArgs e)
        {
            _openedFiles.Remove(e.BaseStream);
            e.BaseStream.Close();
        }
    }
}
