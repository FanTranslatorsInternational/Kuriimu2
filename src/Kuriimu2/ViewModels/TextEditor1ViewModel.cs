using System.Collections.ObjectModel;
using System.Linq;
using Caliburn.Micro;
using Kontract.Interface;
using Kore;

namespace Kuriimu2.ViewModels
{
    public sealed class TextEditor1ViewModel : Screen
    {
        private KoreFile _koreFile;
        private ITextAdapter _adapter;
        private TextEntry _selectedEntry;

        public ObservableCollection<TextEntry> Entries { get; } = new ObservableCollection<TextEntry>();

        public TextEditor1ViewModel(KoreFile kFile)
        {
            DisplayName = "Text Editor 1.x";

            _koreFile = kFile;
            _adapter = _koreFile.Adapter as ITextAdapter;

            foreach (var entry in _adapter.Entries)
            {
                Entries.Add(entry);
            }

            SelectedEntry = Entries.First();
        }

        public TextEntry SelectedEntry
        {
            get => _selectedEntry;
            set
            {
                _selectedEntry = value;
                EditedText = _selectedEntry.EditedText;
                NotifyOfPropertyChange(() => SelectedEntry);
            }
        }

        public string EditedText
        {
            get => _selectedEntry?.EditedText;
            set
            {
                if (_selectedEntry == null) return;
                _selectedEntry.EditedText = value;
                NotifyOfPropertyChange(() => EditedText);
            }
        }

        public string EntryCount => Entries.Count + (Entries.Count > 1 ? " Entries" : " Entry");

        public void AddEntry()
        {
            //Entries.Add(new Entry($"Label {Entries.Count}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")));
            NotifyOfPropertyChange(nameof(EntryCount));
        }
    }
}
