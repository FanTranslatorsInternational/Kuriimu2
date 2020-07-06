using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kontract;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Progress;
using Kontract.Interfaces.Providers;
using Kontract.Models;
using Kontract.Models.IO;

namespace Kore.Managers.Plugins
{
    /// <summary>
    /// A nested <see cref="IPluginManager"/> for passing into plugins and controlling their behaviour.
    /// </summary>
    class SubPluginManager : IPluginManager
    {
        private readonly IPluginManager _parentPluginManager;
        // TODO: Pass on as parent of plugin opened file
        private IStateInfo _stateInfo;

        private readonly IList<IStateInfo> _loadedFiles;

        /// <inheritdoc cref="FileSystemProvider"/>
        public IFileSystemProvider FileSystemProvider { get; }

        public SubPluginManager(IPluginManager parentPluginManager, IFileSystemProvider fileSystemProvider)
        {
            ContractAssertions.IsNotNull(parentPluginManager, nameof(parentPluginManager));
            ContractAssertions.IsNotNull(fileSystemProvider, nameof(fileSystemProvider));

            _parentPluginManager = parentPluginManager;
            FileSystemProvider = fileSystemProvider;

            _loadedFiles = new List<IStateInfo>();
        }

        public void RegisterStateInfo(IStateInfo stateInfo)
        {
            ContractAssertions.IsNotNull(stateInfo, nameof(stateInfo));

            _stateInfo = stateInfo;
        }

        public async Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, IList<string> options = null, IProgressContext progress = null)
        {
            ContractAssertions.IsNotNull(_stateInfo, "stateInfo");

            var loadResult = await _parentPluginManager.LoadFile(fileSystem, path, options, progress);
            if (!loadResult.IsSuccessful)
                return loadResult;

            _loadedFiles.Add(loadResult.LoadedState);

            return loadResult;
        }

        public async Task<LoadResult> LoadFile(IFileSystem fileSystem, UPath path, Guid pluginId, IList<string> options = null, IProgressContext progress = null)
        {
            ContractAssertions.IsNotNull(_stateInfo, "stateInfo");

            var loadResult = await _parentPluginManager.LoadFile(fileSystem, path, pluginId, options, progress);
            if (!loadResult.IsSuccessful)
                return loadResult;

            _loadedFiles.Add(loadResult.LoadedState);

            return loadResult;
        }

        public Task<SaveResult> SaveFile(IStateInfo stateInfo)
        {
            ContractAssertions.IsNotNull(_stateInfo, "stateInfo");

            return _parentPluginManager.SaveFile(stateInfo);
        }

        public Task<SaveResult> SaveFile(IStateInfo stateInfo, IFileSystem fileSystem, UPath savePath)
        {
            ContractAssertions.IsNotNull(_stateInfo, "stateInfo");

            return _parentPluginManager.SaveFile(stateInfo, fileSystem, savePath);
        }

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
    }
}
