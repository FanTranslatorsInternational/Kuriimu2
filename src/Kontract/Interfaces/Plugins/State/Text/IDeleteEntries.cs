using Kontract.Models.Text;

namespace Kontract.Interfaces.Plugins.State.Text
{
    /// <summary>
    /// This interface allows the text adapter to delete entries through the UI.
    /// </summary>
    public interface IDeleteEntries
    {
        /// <summary>
        /// Deletes an entry and allows the plugin to perform any required deletion steps.
        /// </summary>
        /// <param name="entry">The entry to be deleted.</param>
        /// <returns>True if the entry was successfully deleted, False otherwise.</returns>
        bool DeleteEntry(TextEntry entry);
    }
}
