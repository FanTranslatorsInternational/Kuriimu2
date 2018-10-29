using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Caliburn.Micro;
using Kontract.Interfaces;
using Kore;
using Kuriimu2.Interfaces;

namespace Kuriimu2.ViewModels
{
    public sealed class TextEditor2ViewModel : Screen, IFileEditor, ITextEditor
    {
        private IWindowManager _wm = new WindowManager();
        private List<IScreen> _windows = new List<IScreen>();
        private ITextAdapter _adapter;

        public KoreFileInfo KoreFile { get; }
        public ObservableCollection<TextEntry> Entries { get; }

        public bool OriginalTextReadOnly => true;

        public string EntryCount => Entries.Count + (Entries.Count > 1 ? " Entries" : " Entry");

        // Constructor
        public TextEditor2ViewModel(KoreFileInfo koreFile)
        {
            KoreFile = koreFile;

            _adapter = KoreFile.Adapter as ITextAdapter;

            if (_adapter != null)
                Entries = new ObservableCollection<TextEntry>(_adapter.Entries);

            //SelectedEntry = Entries?.First();
        }

        public void AddEntry()
        {
            //Entries.Add(new Entry($"Label {Entries.Count}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")));
            //NotifyOfPropertyChange(nameof(EntryCount));
            //NotifyOfPropertyChange(nameof(Entries));
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
