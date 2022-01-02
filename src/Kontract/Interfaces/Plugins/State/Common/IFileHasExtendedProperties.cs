namespace Kontract.Interfaces.Plugins.State.Common
{
    /// <summary>
    /// This interface allows a plugin to display a custom extended properties dialog.
    /// </summary>
    public interface IFileHasExtendedProperties
    {
        /// <summary>
        /// Opens the extended properties dialog.
        /// </summary>
        /// <returns>True if changes were made, False otherwise.</returns>
        bool ShowFileProperties();
    }
}
