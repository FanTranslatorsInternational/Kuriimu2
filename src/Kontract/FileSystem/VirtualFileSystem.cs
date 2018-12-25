using Kontract.Interfaces.Archive;
using Kontract.Interfaces.VirtualFS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Kontract.FileSystem
{
    public class VirtualFileSystem : IVirtualFSRoot
    {
        private char pathDelimiter = Path.DirectorySeparatorChar;

        private IArchiveAdapter _adapter;
        private List<ArchiveFileInfo> _files;
        private List<(string, ArchiveFileInfo)> _openedFiles;

        public string RootDir { get; private set; }

        public bool CanCreateDirectories => false;

        public bool CanCreateFiles
        {
            get { if (_adapter == null) return false; return _adapter is IArchiveAddFile; }
        }

        public bool CanDeleteDirectories
        {
            get { if (_adapter == null) return false; return _adapter is IArchiveDeleteFile; }
        }

        public bool CanDeleteFiles { get { if (_adapter == null) return false; return _adapter is IArchiveDeleteFile; } }

        private string _tempFolder;

        public VirtualFileSystem(IArchiveAdapter adapter, string tempFolder, string root = "")
        {
            if (string.IsNullOrEmpty(tempFolder)) throw new InvalidOperationException("Temporary Folder path was not given");
            if (root == null) throw new ArgumentNullException("Root directory was null");

            _adapter = adapter ?? throw new ArgumentNullException("Adapter was null");
            _files = adapter.Files;

            RootDir = UnifyPathDelimiters(root);
            _tempFolder = Path.GetFullPath(tempFolder);

            if (!Directory.Exists(_tempFolder))
                Directory.CreateDirectory(_tempFolder);
        }

        public VirtualFileSystem(List<ArchiveFileInfo> files, string tempFolder, string root = "")
        {
            if (string.IsNullOrEmpty(tempFolder)) throw new InvalidOperationException("Temporary Folder path was not given");
            if (root == null) throw new ArgumentNullException("Root directory was null");

            _files = files;

            RootDir = UnifyPathDelimiters(root);
            _tempFolder = Path.GetFullPath(tempFolder);

            if (!Directory.Exists(_tempFolder))
                Directory.CreateDirectory(_tempFolder);
        }

        public IEnumerable<string> EnumerateDirectories()
        {
            var rootParts = SplitPath(RootDir).Count();
            var dirs = _files.
                Where(x => UnifyPathDelimiters(Path.GetDirectoryName(x.FileName)).StartsWith(RootDir)).
                Where(x => SplitPath(x.FileName).Length >= rootParts + 2).
                Select(x => Path.Combine(SplitPath(x.FileName).Where((y, i) => i <= rootParts).ToArray())).
                Distinct();

            return dirs;
        }

        public IEnumerable<string> EnumerateFiles()
        {
            var rootParts = SplitPath(RootDir).Count();
            var files = _files.
                Where(x => SplitPath(x.FileName).Count() == rootParts + 1).
                Where(x => Path.GetDirectoryName(x.FileName) == RootDir).
                Select(x => UnifyPathDelimiters(x.FileName));

            return files;
        }

        public IVirtualFSRoot GetDirectory(string path)
        {
            var relativePath = ResolvePath(Path.Combine(RootDir, UnifyPathDelimiters(path)));

            if (!_files.Any(x => UnifyPathDelimiters(x.FileName).StartsWith(relativePath)))
                throw new DirectoryNotFoundException(path);

            var rootParts = SplitPath(relativePath).Count();
            var dirs = _files.
                Where(x => UnifyPathDelimiters(Path.GetDirectoryName(x.FileName)).StartsWith(relativePath)).
                Where(x => SplitPath(x.FileName).Length == rootParts + 1).
                Select(x => Path.GetDirectoryName(x.FileName)).
                Distinct();

            return new VirtualFileSystem(_files, _tempFolder, dirs.FirstOrDefault(x => x == path));
        }

        public FileStream OpenFile(string filename, FileMode mode)
        {
            var resolvedFilepath = ResolvePath(Path.Combine(RootDir, filename));

            // Try getting file to open
            var afi = _files.FirstOrDefault(x => UnifyPathDelimiters(x.FileName) == resolvedFilepath);
            if (afi == null)
                throw new FileNotFoundException(resolvedFilepath);

            // Check if temporary directoy exists
            var tempPath = Path.Combine(_tempFolder, Path.GetDirectoryName(resolvedFilepath));
            if (!Directory.Exists(tempPath))
                Directory.CreateDirectory(tempPath);

            // Extract file to temporary path
            var file = File.Create(Path.Combine(_tempFolder, resolvedFilepath));

            var bk = afi.FileData.Position;
            afi.FileData.Position = 0;
            afi.FileData.CopyTo(file);
            afi.FileData.Position = bk;

            file.Close();

            // Open file with given FileMode
            return File.Open(Path.Combine(_tempFolder, resolvedFilepath), mode);
        }

        private string[] SplitPath(string path)
        {
            return UnifyPathDelimiters(path).Split(new[] { pathDelimiter }, StringSplitOptions.RemoveEmptyEntries);
        }

        private string UnifyPathDelimiters(string path)
        {
            return path.Replace(pathDelimiter == '/' ? '\\' : '/', pathDelimiter);
        }

        private string ResolvePath(string path)
        {
            var splitted = SplitPath(path);

            var result = new List<string>();
            foreach (var part in splitted)
            {
                switch (part)
                {
                    case "..":
                        if (result.Count <= 0)
                            throw new InvalidOperationException("Path out of virtual root");
                        else
                            result.RemoveAt(result.Count - 1);
                        break;
                    case ".":
                        break;
                    default:
                        result.Add(part);
                        break;
                }
            }

            return Path.Combine(result.ToArray());
        }
    }
}
