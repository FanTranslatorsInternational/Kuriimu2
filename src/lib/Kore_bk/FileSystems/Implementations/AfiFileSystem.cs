using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontract;
using Kontract.Interfaces.Managers.Files;
using Kontract.Interfaces.Managers.Streams;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Archive;
using Kore.Streams;
using Zio;

namespace Kore.FileSystem.Implementations
{
    /// <summary>
    /// Provides a <see cref="IFileSystem"/> for an <see cref="IArchiveState"/>.
    /// </summary>
    class AfiFileSystem : FileSystem
    {
        private readonly IFileState _fileState;
        private readonly ITemporaryStreamProvider _temporaryStreamProvider;

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

        #region Directory API

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
            return ArchiveState.Files.Any(x => x.FilePath.IsInDirectory(path, true));
        }

        /// <inheritdoc />
        protected override void MoveDirectoryImpl(UPath srcPath, UPath destPath)
        {
            if (!DirectoryExists(srcPath))
            {
                throw FileSystemExceptionHelper.NewDirectoryNotFoundException(srcPath);
            }

            foreach (var afi in ArchiveState.Files.Where(x => x.FilePath.IsInDirectory(srcPath, true)))
            {
                ArchiveState.AttemptRename(afi, destPath / afi.FilePath.GetSubDirectory(srcPath).ToRelative());
            }

            GetOrCreateDispatcher().RaiseRenamed(destPath, srcPath);
        }

        /// <inheritdoc />
        protected override void DeleteDirectoryImpl(UPath path, bool isRecursive)
        {
            foreach (var afi in ArchiveState.Files.Where(x => x.FilePath.IsInDirectory(path, true)))
            {
                ArchiveState.AttemptRemoveFile(afi);
            }

            GetOrCreateDispatcher().RaiseDeleted(path);
        }

        #endregion

        #region File API

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
            return ArchiveState.Files.Any(x => x.FilePath == path);
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
            ArchiveState.AttemptRename(file, destPath);

            GetOrCreateDispatcher().RaiseRenamed(destPath, srcPath);
        }

        /// <inheritdoc />
        protected override void DeleteFileImpl(UPath path)
        {
            if (!FileExistsImpl(path))
            {
                throw FileSystemExceptionHelper.NewFileNotFoundException(path);
            }

            var file = GetAfi(path);
            ArchiveState.AttemptRemoveFile(file);

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
            if (mode == FileMode.Open && !fileExists)
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
                        afi.SetFileData(new MemoryStream());
                    }
                    else
                    {
                        afi = ArchiveState.AttemptAddFile(new MemoryStream(), path);
                        GetOrCreateDispatcher().RaiseCreated(path);
                    }
                    break;

                case FileMode.CreateNew:
                    afi = ArchiveState.AttemptAddFile(new MemoryStream(), path);

                    GetOrCreateDispatcher().RaiseCreated(path);
                    break;

                case FileMode.OpenOrCreate:
                    afi = fileExists ? GetAfi(path) : ArchiveState.AttemptAddFile(new MemoryStream(), path);

                    if (fileExists)
                        GetOrCreateDispatcher().RaiseCreated(path);

                    break;

                default:
                    return null;
            }

            // HINT: Ignore file access and share

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
            var fileExists = FileExistsImpl(savePath);
            if (!fileExists && !CanCreateFiles)
            {
                throw FileSystemExceptionHelper.NewFileNotFoundException(savePath);
            }

            // Make sure file exists, if possible
            OpenFileImpl(savePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);

            // Set file data to ArchiveFileInfo
            var afi = GetAfi(savePath);
            afi?.SetFileData(saveData);

            // Dispatch event to watcher
            if (!fileExists && afi != null)
                GetOrCreateDispatcher().RaiseCreated(savePath);
        }

        #endregion

        #region Metadata API

        /// <inheritdoc />
        protected override ulong GetTotalSizeImpl(UPath directoryPath)
        {
            if (!DirectoryExistsImpl(directoryPath))
            {
                throw FileSystemExceptionHelper.NewDirectoryNotFoundException(directoryPath);
            }

            return (ulong)ArchiveState.Files.Where(x => x.FilePath.IsInDirectory(directoryPath, true)).Sum(x => x.FileSize);
        }

        /// <inheritdoc />
        protected override FileEntry GetFileEntryImpl(UPath path)
        {
            return new AfiFileEntry(GetAfi(path));
        }

        /// <inheritdoc />
        protected override IEnumerable<FileEntry> EnumerateFileEntriesImpl(UPath path, string searchPattern)
        {
            return EnumerateFileEntriesInternal(path, SearchPattern.Parse(ref path, ref searchPattern), true);
        }

        /// <inheritdoc />
        protected override IEnumerable<FileEntry> EnumerateAllFileEntriesImpl(UPath path, string searchPattern)
        {
            return EnumerateFileEntriesInternal(path, SearchPattern.Parse(ref path, ref searchPattern), false);
        }

        #endregion

        #region Search API

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

        #endregion

        #region Watch API

        /// <inheritdoc />
        protected override bool CanWatchImpl(UPath path)
        {
            return DirectoryExistsImpl(path);
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

        #endregion

        #region Path API

        /// <inheritdoc />
        protected override string ConvertPathToInternalImpl(UPath path)
        {
            return (SubPath / path.ToRelative()).FullName;
        }

        /// <inheritdoc />
        protected override UPath ConvertPathFromInternalImpl(string innerPath)
        {
            var fullPath = innerPath;
            if (!fullPath.StartsWith(SubPath.FullName) || fullPath.Length > SubPath.FullName.Length && fullPath[SubPath == UPath.Root ? 0 : SubPath.FullName.Length] != UPath.DirectorySeparator)
            {
                // More a safe guard, as it should never happen, but if a delegate filesystem doesn't respect its root path
                // we are throwing an exception here
                throw new InvalidOperationException($"The path `{innerPath}` returned by the delegate filesystem is not rooted to the sub path `{SubPath}`");
            }

            var subPath = fullPath.Substring(SubPath.FullName.Length);
            return subPath == string.Empty ? UPath.Root : new UPath(subPath, true);
        }

        #endregion

        private IEnumerable<UPath> EnumeratePathsInternal(UPath path, SearchPattern searchPattern, bool enumerateDirectories, bool enumerateFiles, bool onlyTopDirectory)
        {
            if (!DirectoryExistsImpl(path))
            {
                throw FileSystemExceptionHelper.NewDirectoryNotFoundException(path);
            }

            var matchedFilePaths = ArchiveState.Files.Select(x => x.FilePath).Where(searchPattern.Match).ToArray();
            var filePaths = matchedFilePaths.Where(x => x.IsInDirectory(path, true)).Select(x => x.GetSubDirectory(path)).ToArray();

            // Collect directory paths
            var directories = new HashSet<UPath>();
            foreach (var filePath in filePaths.Select(x => x.GetDirectory().GetSubDirectory(path).ToRelative()))
            {
                var full = path;
                foreach (var part in filePath.Split())
                {
                    full /= part;
                    directories.Add(full);

                    if (onlyTopDirectory)
                        break;
                }
            }

            // Create final set
            IEnumerable<UPath> result = Array.Empty<UPath>();

            if (enumerateDirectories)
                result = result.Concat(directories);

            if (enumerateFiles)
                result = result.Concat(onlyTopDirectory ? matchedFilePaths.Where(x => x.IsInDirectory(path, false)) : matchedFilePaths);

            return result;
        }

        private IEnumerable<FileEntry> EnumerateFileEntriesInternal(UPath path, SearchPattern searchPattern, bool onlyTopDirectory)
        {
            if (!DirectoryExistsImpl(path))
            {
                throw FileSystemExceptionHelper.NewDirectoryNotFoundException(path);
            }

            var matchedFilePaths = ArchiveState.Files.Where(x => searchPattern.Match(x.FilePath));
            matchedFilePaths = matchedFilePaths.Where(x => x.FilePath.IsInDirectory(path, !onlyTopDirectory));

            return matchedFilePaths.Select(x => new AfiFileEntry(x));
        }

        private IArchiveFileInfo GetAfi(UPath filePath)
        {
            return ArchiveState.Files.FirstOrDefault(x => x.FilePath == filePath);
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
