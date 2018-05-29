using Kore;

namespace Kuriimu2.Interface
{
    /// <summary>
    /// This is the UI editor interface for simplifying usage of editor controls.
    /// </summary>
    internal interface IEditor
    {
        /// <summary>
        /// Provides access to a KoreFile instance ascociated with the editor.
        /// </summary>
        KoreFileInfo KoreFile { get; }

        /// <summary>
        /// Allows an editor to save files.
        /// </summary>
        /// <param name="filename">The file to be saved.</param>
        void Save(string filename = "");
    }
}
