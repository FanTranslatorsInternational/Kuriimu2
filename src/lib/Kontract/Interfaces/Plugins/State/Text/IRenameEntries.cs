using Kontract.Models.Plugins.State.Text;

namespace Kontract.Interfaces.Plugins.State.Text
{
    /// <summary>
    /// This interface allows the text adapter to rename entries through the UI making use of the NameList.
    /// </summary>
    public interface IRenameEntries
    {
        /// <summary>
        /// Renames an info and allows the plugin to perform any required renaming steps.
        /// </summary>
        /// <param name="info">The info being renamed.</param>
        /// <param name="name">The new name to be assigned.</param>
        /// <returns><c>true</c>, if the info was renamed, <c>false</c> otherwise.</returns>
        bool RenameEntry(TextInfo info, string name);
    }
}
