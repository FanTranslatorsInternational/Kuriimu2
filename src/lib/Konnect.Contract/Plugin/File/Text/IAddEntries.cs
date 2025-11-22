using Konnect.Contract.DataClasses.Plugin.File.Text;

namespace Konnect.Contract.Plugin.File.Text;

/// <summary>
/// This interface allows the text adapter to add new entries through the UI.
/// </summary>
public interface IAddEntries : ITextFilePluginState
{
    /// <summary>
    /// Determine if the name of a new entry can be set before adding.
    /// </summary>
    bool CanSetNewEntryName { get; }

    /// <summary>
    /// Creates a new entry and allows the plugin to provide its derived type.
    /// </summary>
    /// <param name="page">The page this new entry is created for. <see langword="null"/>, if no <see cref="ITextFilePluginState.Pager"/> is set.</param>
    /// <returns>TextEntry or a derived type.</returns>
    TextEntry CreateEntry(TextEntryPage? page = null);

    /// <summary>
    /// Adds a newly created entry to the file and allows the plugin to perform any required adding steps.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    /// <param name="page">The page this entry needs to be added to. <see langword="null"/>, if no <see cref="ITextFilePluginState.Pager"/> is set.</param>
    /// <returns><see langword="true"/> if the entry was added, <see langword="false"/> otherwise.</returns>
    bool AddEntry(TextEntry entry, TextEntryPage? page = null);
}