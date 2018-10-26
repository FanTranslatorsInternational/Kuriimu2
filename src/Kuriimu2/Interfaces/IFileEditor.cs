using Kore;

namespace Kuriimu2.Interfaces
{
    /// <summary>
    /// This is the UI editor interface for simplifying usage of editor controls.
    /// </summary>
    internal interface IFileEditor
    {
        /// <summary>
        /// Provides access to the KoreFile instance associated with the editor.
        /// </summary>
        KoreFileInfo KoreFile { get; }

        /// <summary>
        /// Allows the editor to save files.
        /// </summary>
        /// <param name="filename">The file to be saved.</param>
        void Save(string filename = "");
    }
}
