using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Media;
using Caliburn.Micro;
using Kontract.Attributes;
using Kontract.Interfaces.Game;
using Kontract.Interfaces.Text;
using Kore;
using Kuriimu2.Dialogs.Common;
using Kuriimu2.Dialogs.ViewModels;
using Kuriimu2.Interfaces;
using Kuriimu2.Tools;
using Image = System.Drawing.Image;

namespace Kuriimu2.ViewModels
{
    public sealed class TextEditor2ViewModel : Screen, ITextEditor
    {
        private IWindowManager _wm = new WindowManager();
        private List<IScreen> _windows = new List<IScreen>();
        private readonly KoreManager _kore;
        private readonly ITextAdapter _adapter;
        private int _selectedZoomLevel;
        private GameAdapter _selectedGameAdapter;
        private GameAdapter _gameAdapterInstance;

        private TextEntry _selectedEntry;

        public KoreFileInfo KoreFile { get; set; }
        public ObservableCollection<TextEntry> Entries { get; private set; }

        public bool OriginalTextReadOnly => true;

        public string EntryCount => Entries.Count + (Entries.Count > 1 ? " Entries" : " Entry");

        // Constructor
        public TextEditor2ViewModel(KoreManager kore, KoreFileInfo koreFile)
        {
            _kore = kore;
            KoreFile = koreFile;

            _adapter = KoreFile.Adapter as ITextAdapter;
            GameAdapters = _kore.GetAdapters<IGameAdapter>().Select(ga => new GameAdapter(ga)).ToList();

            // TODO: Implement game adapter persistence
            SelectedGameAdapter = GameAdapters.FirstOrDefault();
            SelectedZoomLevel = 1;

            if (Keyboard.IsKeyDown(Key.LeftShift))
            {
                SelectedGameAdapter = GameAdapters.FirstOrDefault(ga => ga.Adapter.GetType().GetCustomAttribute<PluginInfoAttribute>().ID == "84D2BD62-7AC6-459B-B3BB-3A65855135F6") ?? GameAdapters.First();
                SelectedZoomLevel = 2;
            }
            else if (Keyboard.IsKeyDown(Key.LeftAlt))
            {
                SelectedGameAdapter = GameAdapters.FirstOrDefault(ga => ga.Adapter.GetType().GetCustomAttribute<PluginInfoAttribute>().ID == "B344166C-F1BE-49B2-9ADC-38771D0A15DA") ?? GameAdapters.First();
                SelectedZoomLevel = 1;
            }

            // Direct entry loading is now dead since GameAdapters have become a thing
            //if (_adapter != null)
            //    Entries = new ObservableCollection<TextEntry>(_adapter.Entries);

            SelectedEntry = Entries?.FirstOrDefault();
        }

        public List<int> ZoomLevels { get; } = new List<int> { 1, 2, 3, 4, 5 };

        public int SelectedZoomLevel
        {
            get => _selectedZoomLevel;
            set
            {
                if (value == _selectedZoomLevel) return;
                _selectedZoomLevel = value;
                NotifyOfPropertyChange(() => SelectedZoomLevel);
            }
        }

        #region Visibility

        public bool TextEditorCanExportFiles => true;

        public bool TextEditorCanImportFiles => true;

        #endregion


        public IList<GameAdapter> GameAdapters { get; }

        public GameAdapter SelectedGameAdapter
        {
            get => _selectedGameAdapter;
            set
            {
                // Adapter
                _selectedGameAdapter = value;
                // Instantiate a new instance of the adapter.
                if (_selectedGameAdapter != null)
                {
                    _gameAdapterInstance = new GameAdapter((IGameAdapter)Activator.CreateInstance(_selectedGameAdapter.Adapter.GetType()));
                    NotifyOfPropertyChange(() => SelectedGameAdapter);
                }
                // TODO: Implement game adapter persistence

                // Entries
                if (_gameAdapterInstance != null)
                {
                    _gameAdapterInstance.Adapter.Filename = KoreFile.StreamFileInfo.FileName;
                    if (_adapter != null)
                        _gameAdapterInstance.Adapter.LoadEntries(_adapter.Entries);
                    Entries = new ObservableCollection<TextEntry>(_gameAdapterInstance.Adapter.Entries);
                }
                else
                {
                    // Direct entry loading when no game adapters are present (not even the NoGameAdapter)
                    Entries = new ObservableCollection<TextEntry>(_adapter.Entries);
                }
                foreach (var entry in Entries)
                    entry.Edited += (sender, args) =>
                    {
                        KoreFile.HasChanges = true;
                        NotifyOfPropertyChange(() => PreviewImage);
                    };
                NotifyOfPropertyChange(() => Entries);
                NotifyOfPropertyChange(() => PreviewImage);
            }
        }

        public ImageSource PreviewImage
        {
            get
            {
                try
                {
                    if (_gameAdapterInstance?.Adapter is IGenerateGamePreviews generator)
                        return generator.GeneratePreview(SelectedEntry).ToBitmapImage();
                }
                catch (Exception ex)
                {
                    // ignore
                }

                return null;
            }
        }

        public bool AddButtonEnabled => _adapter is IAddEntries;

        public void AddEntry()
        {
            if (!(_adapter is IAddEntries add)) return;

            var entry = add.NewEntry();
            var added = false;

            if (_adapter is IRenameEntries ren)
            {
                var nte = new AddTextEntryViewModel
                {
                    Message = "Enter the name of the new text entry.",
                    //TODO: Implement max name length in the add dialog based on the length from the loaded text adapter
                };
                _windows.Add(nte);

                nte.ValidationCallback = () => new ValidationResult
                {
                    CanClose = Regex.IsMatch(nte.Name, _adapter.NameFilter) && _adapter.Entries.All(e => e.Name != nte.Name),
                    ErrorMessage = $"The '{nte.Name}' name is not valid or already exists."
                };

                if (_wm.ShowDialog(nte) == true && add.AddEntry(entry))
                {
                    entry.Name = nte.Name;
                    added = true;
                }
            }
            else if (add.AddEntry(entry))
                added = true;

            if (added)
            {
                KoreFile.HasChanges = true;
                if (_gameAdapterInstance != null)
                {
                    _gameAdapterInstance.Adapter.LoadEntries(_adapter.Entries);
                    Entries = new ObservableCollection<TextEntry>(_gameAdapterInstance.Adapter.Entries);
                }
                else
                    Entries = new ObservableCollection<TextEntry>(_adapter.Entries);
                foreach (var ent in Entries.Where(e => e.Name == entry.Name))
                    ent.Edited += (sender, args) =>
                    {
                        KoreFile.HasChanges = true;
                        NotifyOfPropertyChange(() => PreviewImage);
                    };
                NotifyOfPropertyChange(() => Entries);
                NotifyOfPropertyChange(() => EntryCount);
                SelectedEntry = entry;
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

        //public void Save(string filename = "")
        //{
        //    try
        //    {
        //        // ;_;
        //        //var entries = _gameAdapter.SaveEntries().ToList();
        //        //for (var i = 0; i < entries.Count; i++)
        //        //{
        //        //    _adapter.Entries[i].EditedText = entries[i].EditedText;
        //        //}

        //        // settle...
        //        foreach (var entry in _gameAdapterInstance.Adapter.SaveEntries())
        //            _adapter.Entries.First(e => e.Name == entry.Name).EditedText = entry.EditedText;

        //        if (filename == string.Empty)
        //            ((ISaveFiles)KoreFile.Adapter).Save(KoreFile.StreamFileInfo.FileName);
        //        else
        //        {
        //            ((ISaveFiles)KoreFile.Adapter).Save(filename);
        //            KoreFile.FileInfo = new FileInfo(filename);
        //        }
        //        KoreFile.HasChanges = false;
        //        NotifyOfPropertyChange(DisplayName);
        //    }
        //    catch (Exception)
        //    {
        //        // Handle on UI gracefully somehow~
        //    }
        //}

        public override void TryClose(bool? dialogResult = null)
        {
            for (var i = _windows.Count - 1; i >= 0; i--)
            {
                var scr = _windows[i];
                scr.TryClose(dialogResult);
                _windows.Remove(scr);
            }
            base.TryClose(dialogResult);
        }
    }

    public sealed class GameAdapter
    {
        public IGameAdapter Adapter { get; }

        public ImageSource Icon => Adapter != null && File.Exists(Adapter.IconPath) ? Image.FromFile(Adapter.IconPath).ToBitmapImage(true) : null;

        public string Name => Adapter.Name;

        public GameAdapter(IGameAdapter adapter)
        {
            Adapter = adapter;
        }
    }
}
