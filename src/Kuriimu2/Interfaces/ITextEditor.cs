using System.Collections.ObjectModel;
using Kontract.Interfaces;
using Kontract.Interfaces.Text;

namespace Kuriimu2.Interfaces
{
    /// <summary>
    /// This is the UI text editor interface that allows access to type specific data.
    /// </summary>
    internal interface ITextEditor
    {
        /// <summary>
        /// Provides access to the Entry list associated with the text editor.
        /// </summary>
        ObservableCollection<TextEntry> Entries { get; }
    }
}
