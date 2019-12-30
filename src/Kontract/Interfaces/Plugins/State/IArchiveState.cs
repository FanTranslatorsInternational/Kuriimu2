using System.Collections.Generic;
using Kontract.Models.Archive;

namespace Kontract.Interfaces.Plugins.State
{
    /// <summary>
    /// Marks the state to be an archive and exposes properties to retrieve and modify file data from the state.
    /// </summary>
    public interface IArchiveState : IPluginState
    {
        /// <summary>
        /// The read-only collection of files the current archive contains.
        /// </summary>
        IReadOnlyList<ArchiveFileInfo> Files { get; }
    }
}
