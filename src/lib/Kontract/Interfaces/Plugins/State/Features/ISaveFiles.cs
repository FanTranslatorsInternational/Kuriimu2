﻿using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Models.FileSystem;
using Kontract.Models.Plugins.State;

namespace Kontract.Interfaces.Plugins.State.Features
{
    /// <summary>
    /// Marks the plugin state as saveable and exposes methods to save the current state.
    /// </summary>
    public interface ISaveFiles
    {
        /// <summary>
        /// Determine if the state got modified.
        /// </summary>
        bool ContentChanged { get; }

        /// <summary>
        /// Try to save the current state to a file.
        /// </summary>
        /// <param name="fileSystem">The file system to save the state into.</param>
        /// <param name="savePath">The new path to the initial file.</param>
        /// <param name="saveContext">The context for this save operation, containing environment instances.</param>
        /// <returns>If the save procedure was successful.</returns>
        Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext);
    }
}
