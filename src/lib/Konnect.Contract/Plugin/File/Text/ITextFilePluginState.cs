using Konnect.Contract.DataClasses.Plugin.File.Text;

namespace Konnect.Contract.Plugin.File.Text
{
    public interface ITextFilePluginState : IFilePluginState
    {
        IReadOnlyList<TextEntry> Texts { get; }

        IReadOnlyList<Guid>? Previews { get; }

        ITextEntryPager? Pager { get; }

        #region Optional feature checks

        public bool CanAddEntry => this is IAddEntries;
        public bool CanDeleteEntry => this is IDeleteEntries;
        public bool CanRenameEntry => this is IRenameEntries;

        #endregion
    }
}
