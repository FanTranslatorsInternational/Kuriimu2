using System.Threading.Tasks;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State.Features;
using Kontract.Models.FileSystem;
using Kontract.Models.Plugins.State;

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

        bool AttemptContentChanged => ((ISaveFiles)this).ContentChanged;

        Task AttemptLoad(IFileSystem fileSystem, UPath filePath, LoadContext loadContext) =>
            ((ILoadFiles)this).Load(fileSystem, filePath, loadContext);

        Task AttemptSave(IFileSystem fileSystem, UPath savePath, SaveContext saveContext) =>
            ((ISaveFiles)this).Save(fileSystem, savePath, saveContext);

        #endregion
    }
}
