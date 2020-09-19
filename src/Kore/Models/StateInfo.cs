using System;
using System.Collections.Generic;
using System.Linq;
using Kontract;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.Identifier;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.IO;
using Kore.FileSystem;

namespace Kore.Models
{
    class StateInfo : IStateInfo
    {
        /// <inheritdoc />
        public IPluginManager PluginManager { get; private set; }

        /// <inheritdoc />
        public IFilePlugin FilePlugin { get; private set; }

        /// <inheritdoc />
        public IPluginState PluginState { get; private set; }

        /// <inheritdoc />
        public UPath AbsoluteDirectory => BuildAbsoluteDirectory();

        /// <inheritdoc />
        public IFileSystem FileSystem { get; private set; }

        /// <inheritdoc />
        public UPath FilePath { get; private set; }

        /// <inheritdoc />
        public IStreamManager StreamManager { get; private set; }

        /// <inheritdoc />
        public IList<IStateInfo> ArchiveChildren { get; private set; }

        /// <inheritdoc />
        public IStateInfo ParentStateInfo { get; private set; }

        /// <inheritdoc />
        public IList<string> DialogOptions { get; private set; }

        /// <inheritdoc />
        public bool HasParent => ParentStateInfo != null;

        /// <inheritdoc />
        public bool StateChanged => IsStateChanged();

        /// <summary>
        /// Represents an open file in the runtime of Kuriimu.
        /// </summary>
        /// <param name="filePlugin">The entry class of the plugin for this file.</param>
        /// <param name="pluginState">The plugin state of this file.</param>
        /// <param name="parentState">The parent state for this file.</param>
        /// <param name="fileSystem">The file system around the initially opened file.</param>
        /// <param name="filePath">The path of the file to be opened in the file system.</param>
        /// <param name="streamManager">The stream manager used for this opened state.</param>
        /// <param name="pluginManager">The plugin manager for this state.</param>
        public StateInfo(IFilePlugin filePlugin, IPluginState pluginState, IStateInfo parentState,
            IFileSystem fileSystem, UPath filePath,
            IStreamManager streamManager, IPluginManager pluginManager)
        {
            ContractAssertions.IsNotNull(filePlugin, nameof(filePlugin));
            ContractAssertions.IsNotNull(pluginState, nameof(pluginState));
            ContractAssertions.IsNotNull(fileSystem, nameof(fileSystem));
            ContractAssertions.IsNotNull(streamManager, nameof(streamManager));
            ContractAssertions.IsNotNull(pluginManager, nameof(pluginManager));

            if (filePath == UPath.Empty || filePath.IsDirectory)
                throw new InvalidOperationException($"'{filePath}' has to be a path to a file.");
            if (!fileSystem.FileExists(filePath))
                throw FileSystemExceptionHelper.NewFileNotFoundException(filePath);

            FilePlugin = filePlugin;
            PluginState = pluginState;
            FilePath = filePath;
            FileSystem = fileSystem;
            StreamManager = streamManager;
            PluginManager = pluginManager;

            ParentStateInfo = parentState;

            ArchiveChildren = new List<IStateInfo>();
        }

        /// <inheritdoc />
        public void SetNewFileInput(IFileSystem fileSystem, UPath filePath)
        {
            FileSystem = fileSystem;
            FilePath = filePath;
        }

        /// <inheritdoc />
        public void SetDialogOptions(IList<string> options)
        {
            ContractAssertions.IsNotNull(options, nameof(options));
            DialogOptions = options;
        }

        /// <inheritdoc />
        public void RenameFilePath(UPath renamedPath)
        {
            FilePath = renamedPath;
        }

        /// <inheritdoc />
        public virtual void Dispose()
        {
            ArchiveChildren?.Clear();
            DialogOptions?.Clear();
            PluginManager?.CloseAll();
            StreamManager?.ReleaseAll();

            FilePlugin = null;
            PluginState = null;
            FilePath = UPath.Empty;
            FileSystem = null;
            StreamManager = null;
            PluginManager = null;
            DialogOptions = null;

            ParentStateInfo = null;
        }

        private bool IsStateChanged()
        {
            if (!(PluginState is ISaveFiles saveState))
                return false;

            return saveState.ContentChanged || ArchiveChildren.Any(child => child.StateChanged);
        }

        private UPath BuildAbsoluteDirectory()
        {
            // AFiFileSystem will return the absolute path over its parents
            return FileSystem.ConvertPathToInternal(UPath.Root);
        }
    }
}
