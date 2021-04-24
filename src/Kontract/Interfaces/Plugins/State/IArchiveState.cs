using System.Collections.Generic;
using System.IO;
using Kontract.Interfaces.Plugins.State.Archive;
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
        
        #region Optional feature support checks
        
        public bool CanReplaceFiles => this is IReplaceFiles;
        public bool CanRenameFiles => this is IRenameFiles;
        public bool CanDeleteFiles => this is IRemoveFiles;
        public bool CanAddFiles => this is IAddFiles;
        
        #endregion
        
        #region Optional feature casting defaults

        void AttemptReplaceFile(IArchiveFileInfo afi, Stream fileData) => ((IReplaceFiles) this).ReplaceFile(afi, fileData);
        void AttemptRename(IArchiveFileInfo afi, UPath path) => ((IRenameFiles) this).Rename(afi, path);
        void AttemptRemoveFile(IArchiveFileInfo afi) => ((IRemoveFiles) this).RemoveFile(afi);
        void AttemptRemoveAll() => ((IRemoveFiles) this).RemoveAll();
        IArchiveFileInfo AttemptAddFile(Stream fileData, UPath filePath) => ((IAddFiles) this).AddFile(fileData, filePath);

        #endregion
    }
}
