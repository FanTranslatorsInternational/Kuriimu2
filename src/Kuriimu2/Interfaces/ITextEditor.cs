using System.Collections.ObjectModel;
using Kontract.Interfaces.Text;

namespace Kuriimu2.Interfaces
{
    /// <inheritdoc />
    /// <summary>
    /// This is the UI text editor interface that allows access to type specific data.
    /// </summary>
    internal interface ITextEditor : IFileEditor
    {
        /// <summary>
        /// Provides access to the Entry list associated with the text editor.
        /// </summary>
        ObservableCollection<TextEntry> Entries { get; }

        /// <summary>
        /// 
        /// </summary>
        bool TextEditorCanExportFiles { get; }

        /// <summary>
        /// 
        /// </summary>
        bool TextEditorCanImportFiles { get; }
    }
}
