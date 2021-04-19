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
using MoreLinq;

namespace Kore.Models
{
    class DefaultFileState : IFileState
    {
        /// <inheritdoc />
        public IFileManager FileManager { get; private set; }

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
        public IList<IFileState> ArchiveChildren { get; private set; }

        /// <inheritdoc />
        public IFileState ParentFileState { get; private set; }

        /// <inheritdoc />
        public IList<string> DialogOptions { get; private set; }

        /// <inheritdoc />
        public bool HasParent => ParentFileState != null;

        /// <inheritdoc />
        public bool StateChanged => IsStateChanged();

        /// <inheritdoc />
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Represents an open file in the runtime of Kuriimu.
        /// </summary>
        /// <param name="filePlugin">The entry class of the plugin for this file.</param>
        /// <param name="pluginState">The plugin state of this file.</param>
        /// <param name="parentFileState">The parent state for this file.</param>
        /// <param name="fileSystem">The file system around the initially opened file.</param>
        /// <param name="filePath">The path of the file to be opened in the file system.</param>
        /// <param name="streamManager">The stream manager used for this opened state.</param>
        /// <param name="fileManager">The plugin manager for this state.</param>
        public DefaultFileState(IFilePlugin filePlugin, IPluginState pluginState, IFileState parentFileState,
            IFileSystem fileSystem, UPath filePath,
            IStreamManager streamManager, IFileManager fileManager)
        {
            ContractAssertions.IsNotNull(filePlugin, nameof(filePlugin));
            ContractAssertions.IsNotNull(pluginState, nameof(pluginState));
            ContractAssertions.IsNotNull(fileSystem, nameof(fileSystem));
            ContractAssertions.IsNotNull(streamManager, nameof(streamManager));
            ContractAssertions.IsNotNull(fileManager, nameof(fileManager));

            if (filePath == UPath.Empty || filePath.IsDirectory)
                throw new InvalidOperationException($"'{filePath}' has to be a path to a file.");
            if (!fileSystem.FileExists(filePath))
                throw FileSystemExceptionHelper.NewFileNotFoundException(filePath);

            FilePlugin = filePlugin;
            PluginState = pluginState;
            FilePath = filePath;
            FileSystem = fileSystem;
            StreamManager = streamManager;
            FileManager = fileManager;

            ParentFileState = parentFileState;

            ArchiveChildren = new List<IFileState>();
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
            FileManager?.CloseAll();
            StreamManager?.ReleaseAll();

            // Dispose content of state
            switch (PluginState)
            {
                case IArchiveState archiveState:
                    archiveState.Files?.ForEach(x => x.Dispose());
                    break;

                case IImageState imageState:
                    imageState.Images?.ForEach(x => x.Dispose());
                    break;
            }

            FilePlugin = null;
            PluginState = null;
            FilePath = UPath.Empty;
            FileSystem = null;
            StreamManager = null;
            FileManager = null;
            DialogOptions = null;

            ParentFileState = null;

            IsDisposed = true;
        }

        private bool IsStateChanged()
        {
            if (!PluginState.CanSave)
                return false;

            return PluginState.ContentChanged || ArchiveChildren.Any(child => child.StateChanged);
        }

        private UPath BuildAbsoluteDirectory()
        {
            // AFiFileSystem will return the absolute path over its parents
            return FileSystem.ConvertPathToInternal(UPath.Root);
        }
    }
}
