using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kontract;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Models;
using Kontract.Models.Archive;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace Kore.Managers.Plugins
{
    /// <summary>
    /// A nested <see cref="IPluginManager"/> for passing into plugins and controlling their behaviour.
    /// </summary>
    class SubPluginManager : IPluginManager
    {
        private readonly PluginManager _parentPluginManager;
        private IStateInfo _stateInfo;

        private readonly IList<IStateInfo> _loadedFiles;

        public SubPluginManager(PluginManager parentPluginManager)
        {
            ContractAssertions.IsNotNull(parentPluginManager, nameof(parentPluginManager));

            _parentPluginManager = parentPluginManager;

            _loadedFiles = new List<IStateInfo>();
        }

        public void RegisterStateInfo(IStateInfo stateInfo)
        {
            ContractAssertions.IsNotNull(stateInfo, nameof(stateInfo));

            _stateInfo = stateInfo;
        }

        #region Load File

        #region Load FileSystem

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path)
        {
            return LoadFile(fileSystem, path, Guid.Empty, new LoadFileContext());
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, LoadFileContext loadFileContext)
        {
            return LoadFile(fileSystem, path, Guid.Empty, loadFileContext);
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, Guid pluginId)
        {
            return LoadFile(fileSystem, path, pluginId, new LoadFileContext());
        }

        /// <inheritdoc />
        public async Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, Guid pluginId, LoadFileContext loadFileContext)
        {
            ContractAssertions.IsNotNull(_stateInfo, "stateInfo");

            // 1. Load file
            var loadResult = await _parentPluginManager.LoadFile(fileSystem, path, pluginId, loadFileContext);
            if (!loadResult.IsSuccessful)
                return loadResult;

            // 2. Add file to loaded files
            _loadedFiles.Add(loadResult.LoadedState);

            return loadResult;
        }

        #endregion

        #region Load ArchiveFileInfo

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IStateInfo stateInfo, ArchiveFileInfo afi)
        {
            return _parentPluginManager.LoadFile(stateInfo, afi);
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IStateInfo stateInfo, ArchiveFileInfo afi, LoadFileContext loadFileContext)
        {
            return _parentPluginManager.LoadFile(stateInfo, afi, loadFileContext);
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IStateInfo stateInfo, ArchiveFileInfo afi, Guid pluginId)
        {
            return _parentPluginManager.LoadFile(stateInfo, afi, pluginId);
        }

        /// <inheritdoc />
        public Task<LoadResult> LoadFile(IStateInfo stateInfo, ArchiveFileInfo afi, Guid pluginId, LoadFileContext loadFileContext)
        {
            return _parentPluginManager.LoadFile(stateInfo, afi, pluginId, loadFileContext);
        }

        #endregion

        #endregion

        #region Save file

        public Task<SaveResult> SaveFile(IStateInfo stateInfo)
        {
            return _parentPluginManager.SaveFile(stateInfo);
        }

        public Task<SaveResult> SaveFile(IStateInfo stateInfo, IFileSystem fileSystem, UPath savePath)
        {
            return _parentPluginManager.SaveFile(stateInfo, fileSystem, savePath);
        }

        #endregion

        #region Close file

        public void Close(IStateInfo stateInfo)
        {
            ContractAssertions.IsElementContained(_loadedFiles, stateInfo, "loadedFiles", nameof(stateInfo));

            _parentPluginManager.Close(stateInfo);
            _loadedFiles.Remove(stateInfo);
        }

        public void CloseAll()
        {
            foreach (var loadedFile in _loadedFiles)
                _parentPluginManager.Close(loadedFile);

            _loadedFiles.Clear();
        }

        #endregion
    }
}
