using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Media;
using Caliburn.Micro;
using Kontract.Interfaces;
using Kore;
using Kore.SamplePlugins;
using Kuriimu2.Interfaces;
using Kuriimu2.Tools;

namespace Kuriimu2.ViewModels
{
    public sealed class TextEditor2ViewModel : Screen, IFileEditor, ITextEditor
    {
        private IWindowManager _wm = new WindowManager();
        private List<IScreen> _windows = new List<IScreen>();
        private ITextAdapter _adapter;
        private GameAdapter _selectedGameAdapter;

        private TextEntry _selectedEntry;

        public Kore.Kore Kore { get; }
        public KoreFileInfo KoreFile { get; }
        public ObservableCollection<TextEntry> Entries { get; private set; }

        public bool OriginalTextReadOnly => true;

        public string EntryCount => Entries.Count + (Entries.Count > 1 ? " Entries" : " Entry");

        // Constructor
        public TextEditor2ViewModel(Kore.Kore kore, KoreFileInfo koreFile)
        {
            Kore = kore;
            KoreFile = koreFile;

            _adapter = KoreFile.Adapter as ITextAdapter;
            GameAdapters = Kore.GetAdapters<IGameAdapter>().Select(ga => new GameAdapter(ga)).ToList();

            // TODO: Implement game adapter persistence
            SelectedGameAdapter = GameAdapters.First(ga => ga.Adapter is VC3GameAdapter);

            //if (_adapter != null)
            //    Entries = new ObservableCollection<TextEntry>(_adapter.Entries);

            SelectedEntry = Entries?.First();
        }

        public IList<GameAdapter> GameAdapters { get; }

        public GameAdapter SelectedGameAdapter
        {
            get => _selectedGameAdapter;
            set
            {
                // Adapter
                _selectedGameAdapter = value;
                NotifyOfPropertyChange(() => SelectedGameAdapter);
                // TODO: Implement game adapter persistence

                // Entries
                _selectedGameAdapter.Adapter.Filename = KoreFile.FileInfo.Name;
                if (_adapter != null)
                    _selectedGameAdapter.Adapter.LoadEntries(_adapter.Entries);
                Entries = new ObservableCollection<TextEntry>(_selectedGameAdapter.Adapter.Entries);
                NotifyOfPropertyChange(() => Entries);
                NotifyOfPropertyChange(() => PreviewImage);
            }
        }

        public TextEntry SelectedEntry
        {
            get => _selectedEntry;
            set
            {
                _selectedEntry = value;
                NotifyOfPropertyChange(() => SelectedEntry);
                NotifyOfPropertyChange(() => PreviewImage);
            }
        }

        public ImageSource PreviewImage
        {
            get
            {
                if (_selectedGameAdapter.Adapter is IGenerateGamePreviews generator)
                    return generator.GeneratePreview(SelectedEntry).ToBitmapImage();

                return null;
            }
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
                // ;_;
                //var entries = _gameAdapter.SaveEntries().ToList();
                //for (var i = 0; i < entries.Count; i++)
                //{
                //    _adapter.Entries[i].EditedText = entries[i].EditedText;
                //}

                // settle...
                foreach (var entry in _selectedGameAdapter.Adapter.SaveEntries())
                    _adapter.Entries.First(e => e.Name == entry.Name).EditedText = entry.EditedText;

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

    public sealed class GameAdapter
    {
        public IGameAdapter Adapter { get; }

        public ImageSource Icon => Adapter != null && File.Exists(Adapter.IconPath) ? Image.FromFile(Adapter.IconPath).ToBitmapImage() : null;

        public string Name => Adapter.Name;

        public GameAdapter(IGameAdapter adapter)
        {
            Adapter = adapter;
        }
    }
}
