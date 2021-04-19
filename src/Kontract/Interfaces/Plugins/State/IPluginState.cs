using System;
using System.Threading.Tasks;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Models.Context;
using Kontract.Models.IO;

namespace Kontract.Interfaces.Plugins.State
{
    /// <summary>
    /// A marker interface that each plugin state has to derive from.
    /// </summary>
    public interface IPluginState
    {
        #region Optional features
        
        /// <summary>
        /// Determine if the state got modified.
        /// </summary>
        bool ContentChanged => false;

        /// <summary>
        /// Try to save the current state to a file.
        /// </summary>
        /// <param name="fileSystem">The file system to save the state into.</param>
        /// <param name="savePath">The new path to the initial file.</param>
        /// <param name="saveContext">The context for this save operation, containing environment instances.</param>
        /// <returns>If the save procedure was successful.</returns>
        Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Load the file into the state.
        /// </summary>
        /// <param name="fileSystem">The file system from which the file is requested.</param>
        /// <param name="filePath">The path to the file requested by the user.</param>
        /// <param name="loadContext">The context for this load operation, containing environment instances.</param>
        /// <returns>If the load procedure was successful.</returns>
        Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            throw new InvalidOperationException();
        }
        
        #endregion
        
        #region Optional feature support checks
        
        public bool CanSave => this.ImplementsMethod(typeof(IPluginState), nameof(Save));
        public bool CanLoad => this.ImplementsMethod(typeof(IPluginState), nameof(Load));

        #endregion
    }
}
