using Konnect.Contract.DataClasses.Plugin.File.Text;

namespace Konnect.Contract.Plugin.File.Text;

/// <summary>
/// This interface allows the text adapter to delete entries through the UI.
/// </summary>
public interface IRemoveEntries : ITextFilePluginState
{
    /// <summary>
    /// Deletes an entry and allows the plugin to perform any required deletion steps.
    /// </summary>
    /// <param name="entry">The entry to be deleted.</param>
    /// <param name="page">The page this entry needs to be deleted from. <see langword="null"/>, if no <see cref="ITextFilePluginState.Pager"/> is set.</param>
    /// <returns><see langword="true"/> if the entry was successfully deleted, <see langword="false"/> otherwise.</returns>
    bool RemoveEntry(TextEntry entry, TextEntryPage? page = null);
}