using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontract;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Interfaces.Providers;
using Kontract.Models.Archive;
using Kontract.Models.IO;
using Kore.Streams;

namespace Kore.FileSystem.Implementations
{
    /// <summary>
    /// Provides a <see cref="IFileSystem"/> for an <see cref="IArchiveState"/>.
    /// </summary>
    class AfiFileSystem : FileSystem
    {
        private readonly IFileState _fileState;
        private readonly ITemporaryStreamProvider _temporaryStreamProvider;

        private readonly IDictionary<UPath, IArchiveFileInfo> _fileDictionary;
        private readonly IDictionary<UPath, (IList<UPath>, IList<IArchiveFileInfo>)> _directoryDictionary;

        // TODO this cast smells, should IFileState/IPluginState be generified?
        protected IArchiveState ArchiveState => _fileState.PluginState as IArchiveState;

        protected UPath SubPath => _fileState.AbsoluteDirectory / _fileState.FilePath.ToRelative();

        /// <summary>
        /// Creates a new instance of <see cref="AfiFileSystem"/>.
        /// </summary>
        /// <param name="fileState">The <see cref="IFileState"/> to retrieve files from.</param>
        /// <param name="streamManager">The stream manager to scope streams in.</param>
        public AfiFileSystem(IFileState fileState, IStreamManager streamManager) : base(streamManager)
        {
            ContractAssertions.IsNotNull(fileState, nameof(fileState));
            if (!(fileState.PluginState is IArchiveState))
                throw new InvalidOperationException("The state is no archive.");

            _fileState = fileState;
            _temporaryStreamProvider = streamManager.CreateTemporaryStreamProvider();

            _fileDictionary = ArchiveState.Files.ToDictionary(x => x.FilePath, y => y);
            _directoryDictionary = CreateDirectoryLookup();
        }

        private AfiFileSystem(IFileState fileState, IStreamManager streamManager, IList<FileSystemWatcher> watchers) :
            this(fileState, streamManager)
        {
            foreach (var watcher in watchers)
                GetOrCreateDispatcher().Add(watcher);
        }

        /// <inheritdoc />
        public override IFileSystem Clone(IStreamManager streamManager)
        {
            return new AfiFileSystem(_fileState, streamManager);
        }

        // ----------------------------------------------
        // Directory API
        // ----------------------------------------------

        /// <inheritdoc />
        public override bool CanCreateDirectories => false;

        /// <inheritdoc />
        public override bool CanDeleteDirectories => ArchiveState.CanDeleteFiles;

        /// <inheritdoc />
        public override bool CanMoveDirectories => ArchiveState.CanRenameFiles;

        /// <inheritdoc />
        protected override void CreateDirectoryImpl(UPath path)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        protected override bool DirectoryExistsImpl(UPath path)
        {
            return _directoryDictionary.ContainsKey(path);
        }

        /// <inheritdoc />
        protected override void MoveDirectoryImpl(UPath srcPath, UPath destPath)
        {
            MoveDirectoryImplInternal(srcPath, destPath);

            GetOrCreateDispatcher().RaiseRenamed(destPath, srcPath);
        }

        private void MoveDirectoryImplInternal(UPath srcPath, UPath destPath)
        {
            if (!DirectoryExists(srcPath))
            {
                throw FileSystemExceptionHelper.NewDirectoryNotFoundException(srcPath);
            }

            var element = _directoryDictionary[srcPath];

            // Move sub directories
            foreach (var subDir in element.Item1.ToArray())
                MoveDirectoryImplInternal(subDir, destPath / subDir.GetName());

            // Move directory
            _directoryDictionary.Remove(srcPath);

            var parent = srcPath.GetDirectory();
            if (!parent.IsNull && !parent.IsEmpty)
                _directoryDictionary[parent].Item1.Remove(srcPath);

            CreateDirectoryInternal(destPath);

            // Move files
            foreach (var file in element.Item2)
            {
                ((IRenameFiles)ArchiveState).Rename(file, destPath / file.FilePath.GetName());
                _directoryDictionary[destPath].Item2.Add(file);
            }
        }

        /// <inheritdoc />
        protected override void DeleteDirectoryImpl(UPath path, bool isRecursive)
        {
            DeleteDirectoryImplInternal(path, isRecursive);

            GetOrCreateDispatcher().RaiseDeleted(path);
        }

        private void DeleteDirectoryImplInternal(UPath path, bool isRecursive)
        {
            if (!DirectoryExistsImpl(path))
            {
                throw FileSystemExceptionHelper.NewDirectoryNotFoundException(path);
            }

            if (!isRecursive && _directoryDictionary[path].Item1.Any())
            {
                throw FileSystemExceptionHelper.NewDirectoryIsNotEmpty(path);
            }

            var element = _directoryDictionary[path];

            // Delete sub directories
            foreach (var subDir in element.Item1.ToArray())
                DeleteDirectoryImplInternal(subDir, true);  // Removing sub directories is always recursive

            // Delete directory
            _directoryDictionary.Remove(path);

            var parent = path.GetDirectory();
            if (!parent.IsNull && !parent.IsEmpty)
                _directoryDictionary[parent].Item1.Remove(path);

            // Delete files
            foreach (var file in element.Item2)
            {
                ((IRemoveFiles)ArchiveState).RemoveFile(file);
            }

            element.Item2.Clear();
        }

        // ----------------------------------------------
        // File API
        // ----------------------------------------------

        /// <inheritdoc />
        public override bool CanCreateFiles => ArchiveState.CanAddFiles;

        /// <inheritdoc />
        // TODO: Maybe finding out how to properly do copying when AFI can either return a normal stream or a temporary one
        public override bool CanCopyFiles => false;

        /// <inheritdoc />
        // TODO: Maybe finding out how to properly do replacing when AFI can either return a normal stream or a temporary one
        public override bool CanReplaceFiles => false;

        /// <inheritdoc />
        public override bool CanMoveFiles => ArchiveState.CanRenameFiles;

        /// <inheritdoc />
        public override bool CanDeleteFiles => ArchiveState.CanDeleteFiles;

        /// <inheritdoc />
        protected override bool FileExistsImpl(UPath path)
        {
            return GetAfi(path) != null;
        }

        /// <inheritdoc />
        protected override void CopyFileImpl(UPath srcPath, UPath destPath, bool overwrite)
        {
            // TODO: Implement copying files
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        protected override void ReplaceFileImpl(UPath srcPath, UPath destPath, UPath destBackupPath, bool ignoreMetadataErrors)
        {
            // TODO: Implement replacing files
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        protected override long GetFileLengthImpl(UPath path)
        {
            if (!FileExistsImpl(path))
            {
                throw FileSystemExceptionHelper.NewFileNotFoundException(path);
            }

            return GetAfi(path).FileSize;
        }

        /// <inheritdoc />
        protected override void MoveFileImpl(UPath srcPath, UPath destPath)
        {
            if (!FileExistsImpl(srcPath))
            {
                throw FileSystemExceptionHelper.NewFileNotFoundException(srcPath);
            }

            var file = GetAfi(srcPath);
            _fileDictionary.Remove(srcPath);

            // Remove file from source directory
            var srcDir = srcPath.GetDirectory();
            _directoryDictionary[srcDir].Item2.Remove(file);

            GetOrCreateDispatcher().RaiseDeleted(srcPath);

            // Rename file
            ((IRenameFiles)ArchiveState).Rename(file, destPath);

            GetOrCreateDispatcher().RaiseRenamed(destPath, srcPath);

            // Create directory of destination
            CreateDirectoryInternal(destPath.GetDirectory());

            // Add file to destination directory
            _directoryDictionary[destPath.GetDirectory()].Item2.Add(file);
            _fileDictionary[destPath] = file;

            GetOrCreateDispatcher().RaiseCreated(destPath);
        }

        /// <inheritdoc />
        protected override void DeleteFileImpl(UPath path)
        {
            if (!FileExistsImpl(path))
            {
                throw FileSystemExceptionHelper.NewFileNotFoundException(path);
            }

            var file = GetAfi(path);

            // Remove file from directory
            var srcDir = path.GetDirectory();
            _directoryDictionary[srcDir].Item2.Remove(file);

            // Remove file
            ((IRemoveFiles)ArchiveState).RemoveFile(file);

            GetOrCreateDispatcher().RaiseDeleted(path);
        }

        /// <inheritdoc />
        protected override Stream OpenFileImpl(UPath path, FileMode mode, FileAccess access, FileShare share)
        {
            return OpenFileAsyncImpl(path, mode, access, share).Result;
        }

        /// <inheritdoc />
        protected override async Task<Stream> OpenFileAsyncImpl(UPath path, FileMode mode, FileAccess access, FileShare share)
        {
            if (mode == FileMode.Append || mode == FileMode.Truncate)
                throw new InvalidOperationException("FileModes 'Append' and 'Truncate' are not supported.");

            var fileExists = FileExistsImpl(path);
            if ((mode == FileMode.Open || mode == FileMode.Truncate) && !fileExists)
            {
                throw FileSystemExceptionHelper.NewFileNotFoundException(path);
            }

            IArchiveFileInfo afi;
            switch (mode)
            {
                case FileMode.Open:
                    afi = GetAfi(path);
                    break;

                case FileMode.Create:
                    if (fileExists)
                    {
                        afi = GetAfi(path);
                        afi?.SetFileData(new MemoryStream());
                    }
                    else
                    {
                        afi = CreateFileInternal(new MemoryStream(), path);
                        GetOrCreateDispatcher().RaiseCreated(path);
                    }
                    break;

                case FileMode.CreateNew:
                    afi = CreateFileInternal(new MemoryStream(), path);

                    GetOrCreateDispatcher().RaiseCreated(path);
                    break;

                case FileMode.OpenOrCreate:
                    afi = fileExists ? GetAfi(path) : CreateFileInternal(new MemoryStream(), path);

                    if (fileExists)
                        GetOrCreateDispatcher().RaiseCreated(path);

                    break;

                default:
                    return null;
            }

            // Ignore file mode, access and share for now
            // TODO: Find a way to somehow allow for mode and access to have an effect?

            // Get data of ArchiveFileInfo
            var afiData = await afi.GetFileData(_temporaryStreamProvider);

            // Wrap data accordingly to not dispose the original ArchiveFileInfo data
            if (!(afiData is TemporaryStream))
                afiData = StreamManager.WrapUndisposable(afiData);

            GetOrCreateDispatcher().RaiseOpened(path);

            afiData.Position = 0;
            return afiData;
        }

        /// <inheritdoc />
        protected override void SetFileDataImpl(UPath savePath, Stream saveData)
        {
            if (!FileExistsImpl(savePath))
            {
                throw FileSystemExceptionHelper.NewFileNotFoundException(savePath);
            }

            GetAfi(savePath)?.SetFileData(saveData);

            GetOrCreateDispatcher().RaiseCreated(savePath);
        }

        // ----------------------------------------------
        // Metadata API
        // ----------------------------------------------

        /// <inheritdoc />
        protected override ulong GetTotalSizeImpl(UPath directoryPath)
        {
            if (!DirectoryExistsImpl(directoryPath))
            {
                throw FileSystemExceptionHelper.NewDirectoryNotFoundException(directoryPath);
            }

            var (directories, files) = _directoryDictionary[directoryPath];

            var totalFileSize = files.Sum(x => x.FileSize);
            var totalDirectorySize = directories.Select(GetTotalSizeImpl).Sum(x => (long)x);
            return (ulong)(totalFileSize + totalDirectorySize);
        }

        /// <inheritdoc />
        protected override FileEntry GetFileEntryImpl(UPath path)
        {
            var afi = GetAfi(path);
            return new AfiFileEntry(afi);
        }

        // ----------------------------------------------
        // Search API
        // ----------------------------------------------

        /// <inheritdoc />
        protected override IEnumerable<UPath> EnumeratePathsImpl(UPath path, string searchPattern, SearchOption searchOption, SearchTarget searchTarget)
        {
            var search = SearchPattern.Parse(ref path, ref searchPattern);

            var onlyTopDirectory = searchOption == SearchOption.TopDirectoryOnly;
            var enumerateDirectories = searchTarget == SearchTarget.Directory;
            var enumerateFiles = searchTarget == SearchTarget.File;

            foreach (var enumeratedPath in EnumeratePathsInternal(path, search, enumerateDirectories, enumerateFiles, onlyTopDirectory).OrderBy(x => x))
                yield return enumeratedPath;
        }

        // ----------------------------------------------
        // Watch API
        // ----------------------------------------------

        /// <inheritdoc />
        protected override bool CanWatchImpl(UPath path)
        {
            return DirectoryExists(path);
        }

        /// <inheritdoc />
        protected override IFileSystemWatcher WatchImpl(UPath path)
        {
            var watcher = new FileSystemWatcher(this, path);
            watcher.Disposed += Watcher_Disposed;

            GetOrCreateDispatcher().Add(watcher);

            return watcher;
        }

        private void Watcher_Disposed(object sender, EventArgs e)
        {
            GetOrCreateDispatcher().Remove((FileSystemWatcher)sender);
        }

        // ----------------------------------------------
        // Path API
        // ----------------------------------------------

        /// <inheritdoc />
        protected override string ConvertPathToInternalImpl(UPath path)
        {
            var safePath = path.ToRelative();
            return (SubPath / safePath).FullName;
        }

        /// <inheritdoc />
        protected override UPath ConvertPathFromInternalImpl(string innerPath)
        {
            var fullPath = innerPath;
            if (!fullPath.StartsWith(SubPath.FullName) || (fullPath.Length > SubPath.FullName.Length && fullPath[SubPath == UPath.Root ? 0 : SubPath.FullName.Length] != UPath.DirectorySeparator))
            {
                // More a safe guard, as it should never happen, but if a delegate filesystem doesn't respect its root path
                // we are throwing an exception here
                throw new InvalidOperationException($"The path `{innerPath}` returned by the delegate filesystem is not rooted to the subpath `{SubPath}`");
            }

            var subPath = fullPath.Substring(SubPath.FullName.Length);
            return subPath == string.Empty ? UPath.Root : new UPath(subPath, true);
        }

        #region Enumerating Paths

        private IEnumerable<UPath> EnumeratePathsInternal(UPath path, SearchPattern searchPattern, bool enumerateDirectories, bool enumerateFiles, bool onlyTopDirectory, bool firstIteration = true)
        {
            if (!DirectoryExistsImpl(path))
                throw FileSystemExceptionHelper.NewDirectoryNotFoundException(path);

            var (directories, files) = _directoryDictionary[path];

            // Enumerate files of current path
            if (enumerateFiles)
            {
                foreach (var file in files.Where(x => searchPattern.Match(x.FilePath)))
                    yield return file.FilePath;
            }

            // Enumerate directories of current path
            if (enumerateDirectories)
            {
                foreach (var directory in directories.Where(searchPattern.Match))
                    yield return directory;
            }

            if (onlyTopDirectory)
                yield break;

            // Enumerate sub directories of current path
            foreach (var directory in directories)
                foreach (var enumeratedPath in EnumeratePathsInternal(directory, searchPattern, enumerateDirectories, enumerateFiles, false, false))
                    yield return enumeratedPath;
        }

        #endregion

        #region Directory tree

        private IDictionary<UPath, (IList<UPath>, IList<IArchiveFileInfo>)> CreateDirectoryLookup()
        {
            var result = new Dictionary<UPath, (IList<UPath>, IList<IArchiveFileInfo>)>
            {
                // Add root manually
                [UPath.Root] = (new List<UPath>(), new List<IArchiveFileInfo>())
            };

            foreach (var file in ArchiveState.Files)
            {
                var path = file.FilePath.GetDirectory();
                CreateDirectoryEntries(result, path);

                result[path].Item2.Add(file);
            }

            return result;
        }

        private IArchiveFileInfo CreateFileInternal(Stream fileData, UPath newFilePath)
        {
            if (!ArchiveState.CanAddFiles)
                return null;

            var newAfi = ((IAddFiles)ArchiveState).AddFile(fileData, newFilePath);
            _fileDictionary[newFilePath] = newAfi;

            CreateDirectoryInternal(newFilePath.GetDirectory());
            var dirEntry = _directoryDictionary[newFilePath.GetDirectory()];
            dirEntry.Item2.Add(newAfi);

            return newAfi;
        }

        private void CreateDirectoryInternal(UPath newPath)
        {
            CreateDirectoryEntries(_directoryDictionary, newPath);
        }

        private void CreateDirectoryEntries(IDictionary<UPath, (IList<UPath>, IList<IArchiveFileInfo>)> directories, UPath newPath)
        {
            var path = UPath.Root;
            foreach (var part in newPath.Split())
            {
                // Initialize parent entry if not existing
                if (!directories.ContainsKey(path))
                    directories[path] = (new List<UPath>(), new List<IArchiveFileInfo>());

                // Add current directory to parent
                if (!directories[path].Item1.Contains(path / part))
                    directories[path].Item1.Add(path / part);

                path /= part;

                // Initialize current directory if not existing
                if (!directories.ContainsKey(path))
                    directories[path] = (new List<UPath>(), new List<IArchiveFileInfo>());
            }
        }

        #endregion

        private IArchiveFileInfo GetAfi(UPath filePath)
        {
            if (!_fileDictionary.ContainsKey(filePath))
                return null;

            var afi = _fileDictionary[filePath];

            // If the file data stream is closed, this may point to a stale reference from before an external operation modified the IFileState
            // Do expensive FirstOrDefault operation to search the requested file in the ArchiveState itself
            if (afi.IsFileDataInvalid)
                afi = ArchiveState.Files.FirstOrDefault(x => x.FilePath == filePath);

            // Update the file dictionary, regardless of validity of the file data stream
            if (afi == null)
                _fileDictionary.Remove(filePath);
            else
                _fileDictionary[filePath] = afi;

            return afi;
        }
    }

    public class AfiFileEntry : FileEntry
    {
        public IArchiveFileInfo ArchiveFileInfo { get; }

        public AfiFileEntry(IArchiveFileInfo afi) : base(afi.FilePath, afi.FileSize)
        {
            ArchiveFileInfo = afi;
        }
    }
}
