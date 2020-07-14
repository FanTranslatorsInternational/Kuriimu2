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
        private readonly IStateInfo _stateInfo;
        private readonly ITemporaryStreamProvider _temporaryStreamProvider;

        protected IArchiveState ArchiveState => _stateInfo.PluginState as IArchiveState;

        protected UPath SubPath=> _stateInfo.AbsoluteDirectory / _stateInfo.FilePath.ToRelative();

        /// <summary>
        /// Creates a new instance of <see cref="AfiFileSystem"/>.
        /// </summary>
        /// <param name="stateInfo">The <see cref="IStateInfo"/> to retrieve files from.</param>
        /// <param name="streamManager">The stream manager to scope streams in.</param>
        public AfiFileSystem(IStateInfo stateInfo, IStreamManager streamManager) : base(streamManager)
        {
            ContractAssertions.IsNotNull(stateInfo, nameof(stateInfo));
            if (!(stateInfo.PluginState is IArchiveState))
                throw new InvalidOperationException("The state is no archive.");

            _stateInfo = stateInfo;
            _temporaryStreamProvider = streamManager.CreateTemporaryStreamProvider();
        }

        /// <inheritdoc />
        public override IFileSystem Clone(IStreamManager streamManager)
        {
            return new AfiFileSystem(_stateInfo, streamManager);
        }

        // ----------------------------------------------
        // Directory API
        // ----------------------------------------------

        /// <inheritdoc />
        public override bool CanCreateDirectories => false;

        /// <inheritdoc />
        public override bool CanDeleteDirectories => ArchiveState is IRemoveFiles;

        /// <inheritdoc />
        protected override void CreateDirectoryImpl(UPath path)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        protected override bool DirectoryExistsImpl(UPath path)
        {
            return ArchiveState.Files.Any(f => f.FilePath.ToString().StartsWith(path.ToString()));
        }

        /// <inheritdoc />
        protected override void MoveDirectoryImpl(UPath srcPath, UPath destPath)
        {
            if (!DirectoryExists(srcPath))
            {
                throw FileSystemExceptionHelper.NewDirectoryNotFoundException(srcPath);
            }

            foreach (var afi in ArchiveState.Files.Where(f => f.FilePath.ToString().StartsWith(srcPath.ToString())))
            {
                afi.FilePath = ReplaceFirst(afi.FilePath.ToString(), srcPath.ToString(), destPath.ToString());
            }
        }

        /// <inheritdoc />
        protected override void DeleteDirectoryImpl(UPath path, bool isRecursive)
        {
            if (!DirectoryExists(path))
            {
                throw FileSystemExceptionHelper.NewDirectoryNotFoundException(path);
            }

            if (!isRecursive)
            {
                throw FileSystemExceptionHelper.NewDirectoryIsNotEmpty(path);
            }

            var removedFiles = ArchiveState.Files.Where(afi => afi.FilePath.ToString().StartsWith(path.ToString())).ToArray();
            var removeArchiveState = ArchiveState as IRemoveFiles;
            foreach (var removedFile in removedFiles)
            {
                removeArchiveState?.RemoveFile(removedFile);
            }
        }

        // ----------------------------------------------
        // File API
        // ----------------------------------------------

        /// <inheritdoc />
        public override bool CanCreateFiles => ArchiveState is IAddFiles;

        /// <inheritdoc />
        // TODO: Maybe finding out how to properly do copying when AFI can either return a normal stream or a temporary one
        public override bool CanCopyFiles => false;

        /// <inheritdoc />
        // TODO: Maybe finding out how to properly do replacing when AFI can either return a normal stream or a temporary one
        public override bool CanReplaceFiles => false;

        /// <inheritdoc />
        public override bool CanDeleteFiles => ArchiveState is IRemoveFiles;

        /// <inheritdoc />
        protected override bool FileExistsImpl(UPath path)
        {
            return ArchiveState.Files.Any(f => f.FilePath == path);
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
            if (!FileExists(path))
            {
                throw FileSystemExceptionHelper.NewFileNotFoundException(path);
            }

            return GetArchiveFileInfo(path).FileSize;
        }

        /// <inheritdoc />
        protected override void MoveFileImpl(UPath srcPath, UPath destPath)
        {
            if (!FileExists(srcPath))
            {
                throw FileSystemExceptionHelper.NewFileNotFoundException(srcPath);
            }

            GetArchiveFileInfo(srcPath).FilePath = destPath;
        }

        /// <inheritdoc />
        protected override void DeleteFileImpl(UPath path)
        {
            if (!FileExists(path))
            {
                throw FileSystemExceptionHelper.NewFileNotFoundException(path);
            }

            var removingState = ArchiveState as IRemoveFiles;
            removingState?.RemoveFile(GetArchiveFileInfo(path));
        }

        /// <inheritdoc />
        protected override Stream OpenFileImpl(UPath path, FileMode mode, FileAccess access, FileShare share)
        {
            if (!FileExists(path))
            {
                throw FileSystemExceptionHelper.NewFileNotFoundException(path);
            }

            // Ignore file mode, access and share for now
            // TODO: Find a way to somehow allow for mode and access to have an effect?

            // 1. Get data of ArchiveFileInfo
            var afi = GetArchiveFileInfo(path);
            var afiData = afi.GetFileData(_temporaryStreamProvider).Result;

            // 2. Wrap data accordingly to not dispose the original ArchiveFileInfo data
            if (!(afiData is TemporaryStream))
                afiData = StreamManager.WrapUndisposable(afiData);

            afiData.Position = 0;

            return afiData;
        }

        /// <inheritdoc />
        protected override async Task<Stream> OpenFileAsyncImpl(UPath path, FileMode mode, FileAccess access, FileShare share)
        {
            if (!FileExists(path))
            {
                throw FileSystemExceptionHelper.NewFileNotFoundException(path);
            }

            // Ignore file mode, access and share for now
            // TODO: Find a way to somehow allow for mode and access to have an effect?

            // 1. Get data of ArchiveFileInfo
            var afi = GetArchiveFileInfo(path);
            var afiData = await afi.GetFileData(_temporaryStreamProvider);

            // 2. Wrap data accordingly to not dispose the original ArchiveFileInfo data
            if (!(afiData is TemporaryStream))
                afiData = StreamManager.WrapUndisposable(afiData);

            afiData.Position = 0;

            return afiData;
        }

        /// <inheritdoc />
        protected override void SetFileDataImpl(UPath savePath, Stream saveData)
        {
            if (!FileExists(savePath))
            {
                throw FileSystemExceptionHelper.NewFileNotFoundException(savePath);
            }

            var afi = GetArchiveFileInfo(savePath);
            afi.SetFileData(saveData);
        }

        // ----------------------------------------------
        // Metadata API
        // ----------------------------------------------

        /// <inheritdoc />
        protected override ulong GetTotalSizeImpl(UPath directoryPath)
        {
            return (ulong)ArchiveState.Files
                .Where(afi => afi.FilePath.GetDirectory().IsInDirectory(directoryPath, true))
                .Sum(x => x.FileSize);
        }

        /// <inheritdoc />
        protected override IEnumerable<UPath> EnumeratePathsImpl(UPath path, string searchPattern, SearchOption searchOption, SearchTarget searchTarget)
        {
            var search = SearchPattern.Parse(ref path, ref searchPattern);

            switch (searchTarget)
            {
                case SearchTarget.File:
                    return EnumerateFiles(search, path, searchOption)
                        .OrderBy(x => x.FullName);

                case SearchTarget.Directory:
                    return EnumerateDirectories(search, path, searchOption)
                        .OrderBy(x => x.FullName);

                case SearchTarget.Both:
                    return EnumerateDirectories(search, path, searchOption)
                        .Concat(EnumerateFiles(search, path, searchOption))
                        .OrderBy(x => x.FullName);
            }

            return Array.Empty<UPath>();
        }

        protected override string ConvertPathToInternalImpl(UPath path)
        {
            var safePath = path.ToRelative();
            return (SubPath / safePath).FullName;
        }

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

        private ArchiveFileInfo GetArchiveFileInfo(UPath path)
        {
            // TODO: Work with lookup
            return ArchiveState.Files.First(f => f.FilePath == path);
        }

        private string ReplaceFirst(string text, string search, string replace)
        {
            var pos = text.IndexOf(search, StringComparison.Ordinal);
            if (pos < 0)
            {
                return text;
            }

            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        private IEnumerable<UPath> EnumerateFiles(SearchPattern searchPattern, UPath topDirectory, SearchOption searchOption)
        {
            switch (searchOption)
            {
                case SearchOption.AllDirectories:
                    return ArchiveState.Files
                        .Where(x => x.FilePath.IsInDirectory(topDirectory, true))
                        .Where(x => searchPattern.Match(x.FilePath))
                        .Select(x => x.FilePath);

                case SearchOption.TopDirectoryOnly:
                    return ArchiveState.Files
                        .Where(x => x.FilePath.IsInDirectory(topDirectory, false))
                        .Where(x => x.FilePath.GetDirectory() == topDirectory && searchPattern.Match(x.FilePath))
                        .Select(x => x.FilePath);
            }

            return Array.Empty<UPath>();
        }

        private IEnumerable<UPath> EnumerateDirectories(SearchPattern searchPattern, UPath topDirectory, SearchOption searchOption)
        {
            switch (searchOption)
            {
                case SearchOption.AllDirectories:
                    return ArchiveState.Files
                        .Where(x => x.FilePath.IsInDirectory(topDirectory, true))
                        .Where(x => searchPattern.Match(x.FilePath.GetDirectory()))
                        .Select(x => x.FilePath.GetDirectory())
                        .Distinct();

                case SearchOption.TopDirectoryOnly:
                    return ArchiveState.Files
                        .Where(x => x.FilePath.IsInDirectory(topDirectory, false))
                        .Where(x => searchPattern.Match(x.FilePath.GetDirectory()))
                        .Select(x => x.FilePath.GetDirectory())
                        .Distinct();
            }

            return Array.Empty<UPath>();
        }
    }
}
