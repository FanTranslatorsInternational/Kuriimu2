using Kontract.Models.Text;

namespace Kontract.Interfaces.Plugins.State.Text
{
    /// <summary>
    /// Entries provide an extended properties dialog?
    /// </summary>
    public interface ITextEntriesHaveExtendedProperties
    {
        // TODO: Figure out how to best implement this feature with WPF.
        /// <summary>
        /// Opens the extended properties dialog for an entry.
        /// </summary>
        /// <param name="entry">The entry to view and/or edit extended properties for.</param>
        /// <returns>True if changes were made, False otherwise.</returns>
        bool ShowEntryProperties(TextEntry entry);
    }
}
