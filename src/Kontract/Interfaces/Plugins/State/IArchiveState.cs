using System;
using System.Collections.Generic;
using System.IO;
using Kontract.Extensions;
using Kontract.Models.Archive;
using Kontract.Models.IO;

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
        IList<IArchiveFileInfo> Files { get; }
        
        #region Optional features

        /// <summary>
        /// Removes a single file from the archive state.
        /// </summary>
        /// <param name="afi">The file to remove.</param>
        /// TODO unused?
        void RemoveFile(IArchiveFileInfo afi)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Removes all files from the archive state.
        /// </summary>
        /// TODO unused?
        void RemoveAll()
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Rename a given file.
        /// </summary>
        /// <param name="afi">The file to rename.</param>
        /// <param name="path">The new path of the file.</param>
        /// TODO: inconsistent naming: should be RenameFile?
        /// TODO unused?
        void Rename(IArchiveFileInfo afi, UPath path)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Replaces file data in a given file.
        /// </summary>
        /// <param name="afi">The file to replace data in.</param>
        /// <param name="fileData">The new file data to replace the original file with.</param>
        /// TODO unused?
        void ReplaceFile(IArchiveFileInfo afi, Stream fileData)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Adds a file to the archive state.
        /// </summary>
        /// <param name="fileData">The file stream to set to the <see cref="IArchiveFileInfo"/>.</param>
        /// <param name="filePath">The path of the file to add to this state.</param>
        /// <returns>The newly created <see cref="ArchiveFileInfo"/>.</returns>
        IArchiveFileInfo AddFile(Stream fileData, UPath filePath)
        {
            throw new InvalidOperationException();
        }
        
        #endregion
        
        #region Optional feature support checks
        
        public bool CanReplaceFiles => this.ImplementsMethod(typeof(IArchiveState), nameof(ReplaceFile));
        public bool CanRenameFiles => this.ImplementsMethod(typeof(IArchiveState), nameof(Rename));
        //TODO also check for RemoveAll?
        public bool CanDeleteFiles => this.ImplementsMethod(typeof(IArchiveState), nameof(RemoveFile));
        public bool CanAddFiles => this.ImplementsMethod(typeof(IArchiveState), nameof(AddFile));
        
        #endregion
    }
}
