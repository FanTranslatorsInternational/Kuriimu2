using Kontract.Models.Plugins.State.Text;

namespace Kontract.Interfaces.Plugins.State.Text
{
    /// <summary>
    /// This interface allows the text adapter to delete entries through the UI.
    /// </summary>
    public interface IDeleteEntries
    {
        /// <summary>
        /// Deletes an info and allows the plugin to perform any required deletion steps.
        /// </summary>
        /// <param name="info">The info to be deleted.</param>
        /// <returns><c>true</c>, if the info was successfully deleted, <c>false</c> otherwise.</returns>
        bool DeleteEntry(TextInfo info);
    }
}
