using System;
using System.Collections.Generic;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.IO;

namespace Kontract.Interfaces.Managers
{
    /// <summary>
    /// Exposes properties for a loaded file in the Kuriimu runtime.
    /// </summary>
    public interface IStateInfo : IDisposable
    {
        /// <summary>
        /// The state of the plugin for this file.
        /// </summary>
        IPluginState State { get; }

        /// <summary>
        /// The path of the file initially opened for this state relative to the file system.
        /// <see cref="UPath.Empty"/> if a file of this format was newly created.
        /// </summary>
        UPath FilePath { get; }

        /// <summary>
        /// The absolute directory of the file system for this state over all parents.
        /// </summary>
        UPath AbsoluteDirectory { get; }

        /// <summary>
        /// The file system <see cref="FilePath"/> is relative to.
        /// The file system is rooted to <see cref="FilePath"/>.
        /// </summary>
        IFileSystem FileSystem { get; }

        /// <summary>
        /// The stream manager for this state.
        /// </summary>
        IStreamManager StreamManager { get; }

        /// <summary>
        /// All child states that were opened from this one.
        /// </summary>
        IList<IStateInfo> ArchiveChildren { get; }

        // TODO: Remove setter
        /// <summary>
        /// The parent state from which this file was opened.
        /// <see langword="null" /> if this file wasn't opened from another state.
        /// </summary>
        IStateInfo ParentStateInfo { get; set; }

        /// <summary>
        /// Gets a value determining if the plugin state changed.
        /// </summary>
        bool StateChanged { get; }

        /// <summary>
        /// Sets <see cref="FileSystem"/> and <see cref="FilePath"/> for a new input file.
        /// </summary>
        /// <param name="fileSystem">The new file system for this state.</param>
        /// <param name="filePath">The new file path relative to the file system for this state.</param>
        void SetNewFileInput(IFileSystem fileSystem, UPath filePath);
    }
}
