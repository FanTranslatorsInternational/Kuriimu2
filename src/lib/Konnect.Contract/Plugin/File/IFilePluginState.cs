using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Font;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Contract.Plugin.File.Text;

namespace Konnect.Contract.Plugin.File;

/// <summary>
/// A marker interface that each plugin state has to derive from.
/// </summary>
public interface IFilePluginState
{
    #region Optional feature support checks

    bool CanLoad => this is ILoadFiles;
    bool CanSave => this is ISaveFiles;
    bool CanCreate => this is ICreateFiles;

    bool IsArchive => this is IArchiveFilePluginState;
    bool IsImage => this is IImageFilePluginState;
    bool IsText => this is ITextFilePluginState;
    bool IsFont => this is IFontFilePluginState;

    #endregion

    #region Optional feature casting defaults

    bool AttemptContentChanged => (this as ISaveFiles)?.ContentChanged ?? false;

    Task AttemptLoad(IFileSystem fileSystem, UPath filePath, LoadContext loadContext) =>
        (this as ILoadFiles)?.Load(fileSystem, filePath, loadContext) ?? Task.CompletedTask;
    Task AttemptSave(IFileSystem fileSystem, UPath savePath, SaveContext saveContext) =>
        (this as ISaveFiles)?.Save(fileSystem, savePath, saveContext) ?? Task.CompletedTask;
    Task AttemptCreate(CreateContext createContext) =>
        (this as ICreateFiles)?.Create(createContext) ?? Task.CompletedTask;

    IArchiveFilePluginState? Archive => this as IArchiveFilePluginState;
    IImageFilePluginState? Image => this as IImageFilePluginState;
    ITextFilePluginState? Text => this as ITextFilePluginState;
    IFontFilePluginState? Font => this as IFontFilePluginState;

    #endregion
}