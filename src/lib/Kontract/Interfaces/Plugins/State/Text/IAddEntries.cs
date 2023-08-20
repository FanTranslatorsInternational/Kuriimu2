using Kontract.Models.Plugins.State.Text;

namespace Kontract.Interfaces.Plugins.State.Text
{
    /// <summary>
    /// This interface allows the text adapter to add new entries through the UI.
    /// </summary>
    public interface IAddEntries
    {
        /// <summary>
        /// Creates a new info and allows the plugin to provide its derived type.
        /// </summary>
        /// <returns><see cref="TextInfo"/> or a derived type.</returns>
        TextInfo CreateNewEntry();

        /// <summary>
        /// Adds a newly created info to the file and allows the plugin to perform any required adding steps.
        /// </summary>
        /// <param name="info"></param>
        /// <returns><c>true</c>, if the info was added, <c>false</c> otherwise.</returns>
        bool AddEntry(TextInfo info);
    }
}
