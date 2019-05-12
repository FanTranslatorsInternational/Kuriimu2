using Kontract.Interfaces.Archive;
using Kontract.Interfaces.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kontract.FileSystem
{
    /// <summary>
    /// 
    /// </summary>
    public class VirtualFileSystem : IFileSystem
    {
        private readonly char _pathDelimiter = Path.DirectorySeparatorChar;

        private IArchiveAdapter _adapter;

        private List<ArchiveFileInfo> _files;

        public string RootDirectory { get; }

        public bool CanCreateDirectories => false;

        public bool CanCreateFiles => _adapter is IArchiveAddFile;

        public bool CanDeleteDirectories => _adapter is IArchiveDeleteFile;

        public bool CanDeleteFiles => _adapter is IArchiveDeleteFile;

        private readonly string _tempFolder;

        public VirtualFileSystem(IArchiveAdapter adapter, string tempFolder, string root = "")
        {
            if (string.IsNullOrEmpty(tempFolder)) throw new InvalidOperationException("Temporary Folder path was not given.");
            if (root == null) throw new ArgumentNullException("Root directory was null.");

            _adapter = adapter ?? throw new ArgumentNullException("Archive Adapter was null.");
            _files = adapter.Files;

            RootDirectory = UnifyPathDelimiters(root);
            _tempFolder = Path.GetFullPath(tempFolder);

            if (!Directory.Exists(_tempFolder))
                Directory.CreateDirectory(_tempFolder);
        }

        public VirtualFileSystem(List<ArchiveFileInfo> files, string tempFolder, string root = "")
        {
            if (string.IsNullOrEmpty(tempFolder)) throw new InvalidOperationException("Temporary Folder path was not given.");
            if (root == null) throw new ArgumentNullException("Root directory was null.");

            _files = files;

            RootDirectory = UnifyPathDelimiters(root);
            _tempFolder = Path.GetFullPath(tempFolder);

            if (!Directory.Exists(_tempFolder))
                Directory.CreateDirectory(_tempFolder);
        }

        public IEnumerable<string> EnumerateDirectories(bool relative = false)
        {
            if (relative) return null;
            //TODO: Relative enumerate

            var rootParts = SplitPath(RootDirectory).Count();
            var dirs = _files
                .Where(x => UnifyPathDelimiters(Path.GetDirectoryName(x.FileName)).StartsWith(RootDirectory))
                .Where(x => SplitPath(x.FileName).Length >= rootParts + 2)
                .Select(x => Path.Combine(SplitPath(x.FileName).Where((y, i) => i <= rootParts).ToArray()))
                .Distinct();

            return dirs;
        }

        public IEnumerable<string> EnumerateFiles(bool relative = false)
        {
            if (relative) return null;
            //TODO: Relative enumerate

            var rootParts = SplitPath(RootDirectory).Count();
            var files = _files
                .Where(x => SplitPath(x.FileName).Count() == rootParts + 1)
                .Where(x => Path.GetDirectoryName(x.FileName) == RootDirectory)
                .Select(x => UnifyPathDelimiters(x.FileName));

            return files;
        }

        public IFileSystem GetDirectory(string path)
        {
            var relativePath = ResolvePath(Path.Combine(RootDirectory, UnifyPathDelimiters(path)));

            if (_files.All(x => Path.GetDirectoryName(UnifyPathDelimiters(x.FileName)) != relativePath))
                throw new DirectoryNotFoundException(path);

            var rootParts = SplitPath(relativePath).Count();
            var dirs = _files
                .Where(x => UnifyPathDelimiters(Path.GetDirectoryName(x.FileName)).StartsWith(relativePath))
                .Where(x => SplitPath(x.FileName).Length == rootParts + 1)
                .Select(x => Path.GetDirectoryName(x.FileName))
                .Distinct();

            return new VirtualFileSystem(_files, _tempFolder, dirs.FirstOrDefault(x => x == path));
        }

        public Stream OpenFile(string filename)
        {
            var resolvedFilepath = ResolvePath(Path.Combine(RootDirectory, filename));

            // Try getting file to open
            var afi = _files.FirstOrDefault(x => UnifyPathDelimiters(x.FileName) == resolvedFilepath);
            if (afi == null)
                throw new FileNotFoundException(resolvedFilepath);

            var fsFileStream = new FileSystemStream(afi.FileData);

            return fsFileStream;
        }

        //public void ExtractFile(string filename)
        //{
        //    var resolvedFilepath = ResolvePath(RelativePath.Combine(RootDir, filename));

        //    // Try getting file to open
        //    var afi = _files.FirstOrDefault(x => UnifyPathDelimiters(x.FileName) == resolvedFilepath);
        //    if (afi == null)
        //        throw new FileNotFoundException(resolvedFilepath);

        //    // Check if temporary directoy exists
        //    var tempPath = RelativePath.Combine(_tempFolder, RelativePath.GetDirectoryName(resolvedFilepath));
        //    if (!Directory.Exists(tempPath))
        //        Directory.CreateDirectory(tempPath);

        //    // Extract file to temporary path
        //    var file = File.Create(RelativePath.Combine(_tempFolder, resolvedFilepath));

        //    var bk = afi.FileData.Position;
        //    afi.FileData.Position = 0;
        //    afi.FileData.CopyTo(file);
        //    afi.FileData.Position = bk;

        //    file.Close();
        //}

        private string[] SplitPath(string path) => UnifyPathDelimiters(path).Split(new[] { _pathDelimiter }, StringSplitOptions.RemoveEmptyEntries);

        private string UnifyPathDelimiters(string path) => path.Replace(_pathDelimiter == '/' ? '\\' : '/', _pathDelimiter);

        private string ResolvePath(string path)
        {
            var parts = SplitPath(path);

            var result = new List<string>();
            foreach (var part in parts)
            {
                switch (part)
                {
                    case "..":
                        if (result.Count <= 0)
                            throw new InvalidOperationException("RelativePath out of virtual root.");
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

        public void Dispose()
        {
            _adapter = null;
            _files = null;
        }

        public Stream CreateFile(string filename)
        {
            if (!CanCreateFiles)
                throw new InvalidOperationException("Can't create files.");

            var createdFile = File.Create(Path.Combine(_tempFolder, RootDirectory, filename));

            var afi = new ArchiveFileInfo { FileData = createdFile, FileName = Path.Combine(RootDirectory, filename), State = ArchiveFileState.Added };
            (_adapter as IArchiveAddFile).AddFile(afi);

            return createdFile;
        }

        public void DeleteFile(string filename)
        {
            if (!CanDeleteFiles)
                throw new InvalidOperationException("Can't delete files.");

            var resolvedFilepath = ResolvePath(Path.Combine(RootDirectory, filename));

            // Try getting file to delete
            var afi = _files.FirstOrDefault(x => UnifyPathDelimiters(x.FileName) == resolvedFilepath);
            if (afi == null)
                throw new FileNotFoundException(resolvedFilepath);

            (_adapter as IArchiveDeleteFile).DeleteFile(afi);
        }

        public bool FileExists(string filename)
        {
            var resolvedFilepath = ResolvePath(Path.Combine(RootDirectory, filename));

            // Try getting file
            var afi = _files.FirstOrDefault(x => UnifyPathDelimiters(x.FileName) == resolvedFilepath);

            return afi != null;
        }
    }
}
