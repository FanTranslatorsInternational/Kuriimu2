using Kontract.Interfaces.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kontract.FileSystem
{
    public class PhysicalFileSystem : IFileSystem
    {
        private readonly List<Stream> _openedFiles;

        public string RootDirectory { get; }

        public bool CanCreateDirectories => CheckWritePermission();

        public bool CanCreateFiles => CheckWritePermission();

        public bool CanDeleteDirectories => CheckWritePermission();

        public bool CanDeleteFiles => CheckWritePermission();

        public PhysicalFileSystem(string root)
        {
            RootDirectory = Path.GetFullPath(root);

            if (!Directory.Exists(RootDirectory))
                Directory.CreateDirectory(RootDirectory);

            _openedFiles = new List<Stream>();
        }

        public IEnumerable<string> EnumerateDirectories(bool relative = false)
        {
            return !relative ? Directory.EnumerateDirectories(RootDirectory) : Directory.EnumerateDirectories(RootDirectory).Select(x => x.Remove(0, RootDirectory.Length + 1));
        }

        public IEnumerable<string> EnumerateFiles(bool relative = false)
        {
            return !relative ? Directory.EnumerateFiles(RootDirectory) : Directory.EnumerateFiles(RootDirectory).Select(x => x.Remove(0, RootDirectory.Length + 1));
        }

        public IFileSystem GetDirectory(string path) => new PhysicalFileSystem(Path.GetFullPath(Path.Combine(RootDirectory, path)));

        public Stream OpenFile(string filename)
        {
            var openedFile = File.Open(Path.Combine(RootDirectory, filename), FileMode.Open);
            var fsFileStream = new FileSystemStream(openedFile);
            fsFileStream.CloseStream += FsFileStream_CloseStream;

            _openedFiles.Add(openedFile);

            return fsFileStream;
        }

        public Stream CreateFile(string filename)
        {
            if (!CanCreateFiles)
                throw new InvalidOperationException("Can't create files.");

            var createdFile = File.Create(Path.Combine(RootDirectory, filename));
            var fsFileStream = new FileSystemStream(createdFile);
            fsFileStream.CloseStream += FsFileStream_CloseStream;

            _openedFiles.Add(createdFile);

            return fsFileStream;
        }

        public void DeleteFile(string filename)
        {
            if (!CanDeleteFiles)
                throw new InvalidOperationException("Can't delete files.");

            File.Delete(Path.Combine(RootDirectory, filename));
        }

        public bool FileExists(string filename)
        {
            return File.Exists(Path.Combine(RootDirectory, filename));
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
                Directory.GetAccessControl(RootDirectory);
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
