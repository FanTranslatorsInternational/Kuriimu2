using Kore;
using Kore.Files.Models;

namespace Kuriimu2.Wpf.Interfaces
{
    /// <summary>
    /// This is the UI editor interface for simplifying usage of editor controls.
    /// </summary>
    internal interface IFileEditor
    {
        /// <summary>
        /// Provides access to the KoreFile instance associated with the editor.
        /// </summary>
        KoreFileInfo KoreFile { get; set;  }
    }
}
