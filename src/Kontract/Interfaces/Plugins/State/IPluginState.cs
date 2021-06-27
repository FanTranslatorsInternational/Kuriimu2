using System.Threading.Tasks;
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
        #region Optional feature support checks
        
        public bool CanSave => this is ISaveFiles;
        public bool CanLoad => this is ILoadFiles;

        #endregion
        
        #region Optional feature casting defaults

        bool AttemptContentChanged => ((ISaveFiles) this).ContentChanged;

        Task AttemptSave(IFileSystem fileSystem, UPath savePath, SaveContext saveContext) =>
            ((ISaveFiles) this).Save(fileSystem, savePath, saveContext);

        Task AttemptLoad(IFileSystem fileSystem, UPath filePath, LoadContext loadContext) =>
            ((ILoadFiles) this).Load(fileSystem, filePath, loadContext);

        #endregion
    }
}
