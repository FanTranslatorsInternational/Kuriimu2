using Konnect.Contract.DataClasses.Plugin.File.Text;

namespace Konnect.Contract.Plugin.File.Text;

public interface ITextFilePluginState : IFilePluginState
{
    IReadOnlyList<TextEntry> Texts { get; }

    IReadOnlyList<Guid>? PreviewGuids { get; }

    ITextEntryPager? Pager { get; }

    #region Optional feature checks

    bool CanAddEntry => this is IAddEntries;
    bool CanRemoveEntry => this is IRemoveEntries;
    bool CanRenameEntry => this is IRenameEntries;

    #endregion

    #region Optional feature casting defaults

    bool AttemptCanSetNewEntryName => (this as IAddEntries)?.CanSetNewEntryName ?? false;

    TextEntry? AttemptCreateEntry(TextEntryPage? page = null) => (this as IAddEntries)?.CreateEntry(page);
    bool AttemptAddEntry(TextEntry entry, TextEntryPage? page = null) => (this as IAddEntries)?.AddEntry(entry, page) ?? false;
    bool AttemptRemoveEntry(TextEntry entry, TextEntryPage? page = null) => (this as IRemoveEntries)?.RemoveEntry(entry, page) ?? false;
    bool AttemptRenameEntry(TextEntry entry, string name) => (this as IRenameEntries)?.RenameEntry(entry, name) ?? false;

    #endregion
}