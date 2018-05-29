namespace Kontract.Interfaces
{
    /// <summary>
    /// This interface allows a plugin to display a custom extended properties dialog.
    /// </summary>
    public interface IFileHasExtendedProperties
    {
        // TODO: Figure out how to best implement this feature with WPF.
        /// <summary>
        /// Opens the extended properties dialog.
        /// </summary>
        /// <returns>True if changes were made, False otherwise.</returns>
        bool ShowFileProperties();
    }
}
