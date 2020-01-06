using System;
using System.Collections.Generic;
using System.Linq;
using Kontract;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.IO;
using Kore.FileSystem;

namespace Kore.Models
{
    class StateInfo : IStateInfo
    {
        /// <inheritdoc />
        public IPluginState State { get; private set; }

        /// <inheritdoc />
        public UPath FilePath { get; private set; }

        /// <inheritdoc />
        public IFileSystem FileSystem { get; private set; }

        /// <inheritdoc />
        public IStreamManager StreamManager { get; private set; }

        /// <inheritdoc />
        public IList<IStateInfo> ArchiveChildren { get; private set; }

        /// <inheritdoc />
        public IStateInfo ParentStateInfo { get; set; }

        /// <inheritdoc />
        public UPath SubPath { get; set; }

        public bool StateChanged => IsStateChanged();

        /// <summary>
        /// Represents an open file in the runtime of Kuriimu.
        /// </summary>
        /// <param name="pluginState">The plugin state of this file.</param>
        /// <param name="fileSystem">The file system around the initially opened file.</param>
        /// <param name="filePath">The path of the file to be opened.</param>
        /// <param name="streamManager">The stream manager used for this opened state.</param>
        /// <param name="parentStateInfo">The parent state info from which this file got opened.</param>
        /// <param name="subPath">The sub directory in relation to the parent file system.</param>
        public StateInfo(IPluginState pluginState, IFileSystem fileSystem, UPath filePath, IStreamManager streamManager, IStateInfo parentStateInfo = null, UPath? subPath = null)
        {
            ContractAssertions.IsNotNull(pluginState, nameof(pluginState));
            ContractAssertions.IsNotNull(fileSystem, nameof(fileSystem));
            ContractAssertions.IsNotNull(streamManager, nameof(streamManager));

            if (filePath == UPath.Empty || filePath.IsDirectory)
                throw new InvalidOperationException($"'{filePath}' has to be a path to a file.");
            if (!fileSystem.FileExists(filePath))
                throw FileSystemExceptionHelper.NewFileNotFoundException(filePath);

            State = pluginState;
            FilePath = filePath;
            FileSystem = fileSystem;
            StreamManager = streamManager;

            ParentStateInfo = parentStateInfo;
            SubPath = subPath ?? UPath.Empty;

            ArchiveChildren = new List<IStateInfo>();
        }

        public virtual void Dispose()
        {
            ArchiveChildren?.Clear();
            StreamManager?.ReleaseAll();

            State = null;
            FilePath = UPath.Empty;
            FileSystem = null;
            StreamManager = null;

            ParentStateInfo = null;
            SubPath = UPath.Empty;
        }

        private bool IsStateChanged()
        {
            if (!(State is ISaveFiles saveState))
                return false;

            return saveState.ContentChanged || ArchiveChildren.Any(child => child.StateChanged);
        }
    }
}
