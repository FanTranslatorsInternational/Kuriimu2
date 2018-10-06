using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Caliburn.Micro;
using Kontract.Interfaces;
using Kore;
using Kuriimu2.Interface;

namespace Kuriimu2.ViewModels
{
    public sealed class TextEditor1ViewModel : Screen, IFileEditor
    {
        private ITextAdapter _adapter;

        public KoreFileInfo KoreFile { get; }
        public ObservableCollection<TextEntry> Entries { get; }

        private TextEntry _selectedEntry;

        public TextEditor1ViewModel(KoreFileInfo koreFile)
        {
            KoreFile = koreFile;

            DisplayName = KoreFile.DisplayName.Replace("_", "__");
            _adapter = KoreFile.Adapter as ITextAdapter;
            
            if (_adapter != null)
                Entries = new ObservableCollection<TextEntry>(_adapter.Entries);

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
            NotifyOfPropertyChange(nameof(Entries));
        }

        public void Save(string filename = "")
        {
            try
            {
                if (filename == string.Empty)
                    ((ISaveFiles)KoreFile.Adapter).Save(KoreFile.FileInfo.FullName);
                else
                {
                    ((ISaveFiles)KoreFile.Adapter).Save(filename);
                    KoreFile.FileInfo = new FileInfo(filename);
                }
                KoreFile.HasChanges = false;
                NotifyOfPropertyChange(DisplayName);
            }
            catch (Exception)
            {
                // Handle on UI gracefully somehow~
            }
        }
    }
}
