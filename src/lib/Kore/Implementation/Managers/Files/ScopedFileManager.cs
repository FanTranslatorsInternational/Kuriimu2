using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kontract;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers.Files;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Models.FileSystem;
using Kontract.Models.Managers.Files;

namespace Kore.Implementation.Managers.Files
{
    /// <summary>
    /// A nested <see cref="IFileManager"/> for passing into plugins and controlling their behaviour.
    /// </summary>
    class ScopedFileManager : IFileManager
    {
        private readonly IKoreFileManager _parentFileManager;
        private IFileState _fileState;

        private readonly IList<IFileState> _loadedFiles;

        public ScopedFileManager(IKoreFileManager parentFileManager)
        {
            ContractAssertions.IsNotNull(parentFileManager, nameof(parentFileManager));

            _parentFileManager = parentFileManager;

            _loadedFiles = new List<IFileState>();
        }

        public void RegisterStateInfo(IFileState fileState)
        {
            ContractAssertions.IsNotNull(fileState, nameof(fileState));

            _fileState = fileState;
        }

        #region Check

        /// <inheritdoc />
        public bool IsLoading(UPath filePath)
        {
            return _parentFileManager.IsLoading(filePath);
        }

        /// <inheritdoc />
        public bool IsLoaded(UPath filePath)
        {
            return _parentFileManager.IsLoaded(filePath);
        }

        /// <inheritdoc />
        public bool IsSaving(IFileState fileState)
        {
            return _parentFileManager.IsSaving(fileState);
        }

        /// <inheritdoc />
        public bool IsClosing(IFileState fileState)
        {
            return _parentFileManager.IsClosing(fileState);
        }

        #endregion

        #region Identify file

        public Task<bool> CanIdentify(IFileState fileState, IArchiveFileInfo afi, Guid pluginId)
        {
            return _parentFileManager.CanIdentify(fileState, afi, pluginId);
        }

        public Task<bool> CanIdentify(StreamFile streamFile, Guid pluginId)
        {
            return _parentFileManager.CanIdentify(streamFile, pluginId);
        }

        public Task<bool> CanIdentify(IFileSystem fileSystem, UPath path, Guid pluginId)
        {
            return _parentFileManager.CanIdentify(fileSystem, path, pluginId);
        }

        #endregion

        #region Load File

        #region Load FileSystem

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path)
        {
            return LoadFile(fileSystem, path, new LoadFileContext());
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, Guid pluginId)
        {
            return LoadFile(fileSystem, path, new LoadFileContext { PluginId = pluginId });
        }

        /// <inheritdoc />
        public async Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, LoadFileContext loadFileContext)
        {
            ContractAssertions.IsNotNull(_fileState, "fileState");

            // If the same file is passed to another plugin, take the parent of the current state
            var parent = _fileState;
            var statePath = _fileState.AbsoluteDirectory / _fileState.FilePath.ToRelative();
            if (fileSystem.ConvertPathToInternal(path) == statePath)
                parent = _fileState.ParentFileState;

            // 1. Load file
            var loadResult = await _parentFileManager.LoadFile(fileSystem, path, parent, loadFileContext);
            if (!loadResult.IsSuccessful)
                return loadResult;

            // 2. Add file to loaded files
            _loadedFiles.Add(loadResult.LoadedFileState);

            return loadResult;
        }

        #endregion

        #region Load ArchiveFileInfo

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IFileState fileState, IArchiveFileInfo afi)
        {
            return _parentFileManager.LoadFile(fileState, afi);
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IFileState fileState, IArchiveFileInfo afi, Guid pluginId)
        {
            return _parentFileManager.LoadFile(fileState, afi, new LoadFileContext { PluginId = pluginId });
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IFileState fileState, IArchiveFileInfo afi, LoadFileContext loadFileContext)
        {
            return _parentFileManager.LoadFile(fileState, afi, loadFileContext);
        }

        #endregion

        #region Load Stream

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(StreamFile streamFile)
        {
            return LoadFile(streamFile, new LoadFileContext());
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(StreamFile streamFile, Guid pluginId)
        {
            return LoadFile(streamFile, new LoadFileContext { PluginId = pluginId });
        }

        /// <inheritdoc />
        public async Task<LoadResult> LoadFile(StreamFile streamFile, LoadFileContext loadFileContext)
        {
            ContractAssertions.IsNotNull(_fileState, "fileState");

            // 1. Load file
            var loadResult = await _parentFileManager.LoadFile(streamFile, loadFileContext);
            if (!loadResult.IsSuccessful)
                return loadResult;

            // 2. Add file to loaded files
            _loadedFiles.Add(loadResult.LoadedFileState);

            return loadResult;
        }

        #endregion

        #endregion

        #region Save File

        public Task<SaveResult> SaveFile(IFileState fileState)
        {
            return _parentFileManager.SaveFile(fileState);
        }

        public Task<SaveResult> SaveFile(IFileState fileState, IFileSystem fileSystem, UPath savePath)
        {
            return _parentFileManager.SaveFile(fileState, fileSystem, savePath);
        }

        #endregion

        #region Save Stream

        public Task<SaveStreamResult> SaveStream(IFileState fileState)
        {
            return _parentFileManager.SaveStream(fileState);
        }

        #endregion

        #region Close file

        public CloseResult Close(IFileState fileState)
        {
            ContractAssertions.IsElementContained(_loadedFiles, fileState, "loadedFiles", nameof(fileState));

            var closeResult = _parentFileManager.Close(fileState);
            _loadedFiles.Remove(fileState);

            return closeResult;
        }

        public void CloseAll()
        {
            foreach (var loadedFile in _loadedFiles)
                _parentFileManager.Close(loadedFile);

            _loadedFiles.Clear();
        }

        #endregion
    }
}
