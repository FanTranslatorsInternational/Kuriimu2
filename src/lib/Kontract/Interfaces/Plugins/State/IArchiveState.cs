using System.Collections.Generic;
using System.IO;
using Kontract.Interfaces.Plugins.State.Archive;
using Kontract.Models.FileSystem;

namespace Kontract.Interfaces.Plugins.State
{
    /// <summary>
    /// Marks the state to be an archive format and exposes properties to retrieve and modify file data from the state.
    /// </summary>
    public interface IArchiveState : IPluginState
    {
        /// <summary>
        /// The read-only collection of files the current archive contains.
        /// </summary>
        IReadOnlyList<IArchiveFileInfo> Files { get; }
        
        #region Optional feature support checks
        
        /// <summary>
        /// If the plugin can replace files.
        /// </summary>
        public bool CanReplaceFiles => this is IReplaceFiles;

        /// <summary>
        /// If the plugin can rename files.
        /// </summary>
        public bool CanRenameFiles => this is IRenameFiles;

        /// <summary>
        /// If the plugin can delete files.
        /// </summary>
        public bool CanDeleteFiles => this is IRemoveFiles;

        /// <summary>
        /// If the plugin can add files.
        /// </summary>
        public bool CanAddFiles => this is IAddFiles;

        #endregion

        #region Optional feature casting defaults

        /// <summary>
        /// Casts and executes <see cref="IReplaceFiles.ReplaceFile"/>.
        /// </summary>
        /// <param name="afi">The file to replace data in.</param>
        /// <param name="fileData">The new file data to replace the original file with.</param>
        void AttemptReplaceFile(IArchiveFileInfo afi, Stream fileData) => ((IReplaceFiles) this).ReplaceFile(afi, fileData);

        /// <summary>
        /// Casts and executes <see cref="IRenameFiles.RenameFile"/>.
        /// </summary>
        /// <param name="afi">The file to rename.</param>
        /// <param name="path">The new path of the file.</param>
        void AttemptRename(IArchiveFileInfo afi, UPath path) => ((IRenameFiles) this).RenameFile(afi, path);

        /// <summary>
        /// Casts and executes <see cref="IRemoveFiles.RemoveFile"/>.
        /// </summary>
        /// <param name="afi">The file to remove.</param>
        void AttemptRemoveFile(IArchiveFileInfo afi) => ((IRemoveFiles) this).RemoveFile(afi);

        /// <summary>
        /// Casts and executes <see cref="IRemoveFiles.RemoveAll"/>.
        /// </summary>
        void AttemptRemoveAll() => ((IRemoveFiles) this).RemoveAll();

        /// <summary>
        /// Casts and executes <see cref="IAddFiles.AddFile"/>.
        /// </summary>
        /// <param name="fileData">The file stream to set to the <see cref="IArchiveFileInfo"/>.</param>
        /// <param name="filePath">The path of the file to add to this state.</param>
        /// <returns>The newly created <see cref="IArchiveFileInfo"/>.</returns>
        IArchiveFileInfo AttemptAddFile(Stream fileData, UPath filePath) => ((IAddFiles) this).AddFile(fileData, filePath);

        #endregion
    }
}
