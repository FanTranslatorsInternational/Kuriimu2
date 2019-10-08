using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Caliburn.Micro;
using Kontract.Interfaces;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Text;
using Kore;
using Kore.Files.Models;
using Kuriimu2.Wpf.Interfaces;

namespace Kuriimu2.Wpf.ViewModels
{
    public sealed class TextEditor1ViewModel : Screen, ITextEditor
    {
        private ITextAdapter _adapter;

        public KoreFileInfo KoreFile { get; set; }
        public ObservableCollection<TextEntry> Entries { get; }

        private TextEntry _selectedEntry;

        // Constructor
        public TextEditor1ViewModel(KoreFileInfo koreFile)
        {
            KoreFile = koreFile;

            DisplayName = KoreFile.DisplayName;
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

        public bool TextEditorCanExportFiles => false;

        public bool TextEditorCanImportFiles => false;

        public void AddEntry()
        {
            //Entries.Add(new Entry($"Label {Entries.Count}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")));
            //NotifyOfPropertyChange(nameof(EntryCount));
            //NotifyOfPropertyChange(nameof(Entries));
        }

        //public void Save(FileManager fileManager, string filename = "")
        //{
        //    try
        //    {
        //        if (!KoreFile.HasChanges && filename == string.Empty)
        //            return;

        //        var ksi = new KoreSaveInfo(KoreFile, "temp") { NewSaveFile = filename };
        //        var savedKfi = fileManager.SaveFile(ksi);

        //        if (savedKfi.ParentKfi != null)
        //            savedKfi.ParentKfi.HasChanges = true;

        //        //if (filename == string.Empty)
        //        //    ((ISaveFiles)KoreFile.Adapter).Save(KoreFile.StreamFileInfo.FileName);
        //        //else
        //        //{
        //        //    ((ISaveFiles)KoreFile.Adapter).Save(filename);
        //        //    KoreFile.FileInfo = new FileInfo(filename);
        //        //}
        //        KoreFile.HasChanges = false;
        //        NotifyOfPropertyChange(DisplayName);
        //    }
        //    catch (Exception)
        //    {
        //        // Handle on UI gracefully somehow~
        //    }
        //}
    }
}
