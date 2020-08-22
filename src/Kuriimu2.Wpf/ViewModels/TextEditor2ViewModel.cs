using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Caliburn.Micro;
using Kontract.Attributes;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Game;
using Kontract.Interfaces.Plugins.State.Text;
using Kontract.Models.Text;
using Kore.Extensions;
using Kore.Managers.Plugins;
using Kuriimu2.Wpf.Dialogs.Common;
using Kuriimu2.Wpf.Dialogs.ViewModels;
using Kuriimu2.Wpf.Interfaces;
using Kuriimu2.Wpf.Tools;
using Image = System.Drawing.Image;

namespace Kuriimu2.Wpf.ViewModels
{
    public sealed class TextEditor2ViewModel : Screen, ITextEditor
    {
        private IWindowManager _wm = new WindowManager();
        private List<IScreen> _windows = new List<IScreen>();
        private readonly PluginManager _pluginManager;
        private readonly ITextState _state;
        private int _selectedZoomLevel;
        private GameAdapter _selectedGameAdapter;
        private GameAdapter _gameAdapterInstance;

        private TextEntry _selectedEntry;

        public IStateInfo KoreFile { get; set; }
        public ObservableCollection<TextEntry> Entries { get; private set; }

        public bool OriginalTextReadOnly => true;

        public string EntryCount => Entries.Count + (Entries.Count > 1 ? " Entries" : " Entry");

        // Constructor
        public TextEditor2ViewModel(PluginManager pluginManager, IStateInfo koreFile)
        {
            _pluginManager = pluginManager;
            KoreFile = koreFile;

            _state = KoreFile.PluginState as ITextState;
            GameAdapters = pluginManager.GetGameAdapters().Select(ga => new GameAdapter(ga)).ToList();

            // TODO: Implement game adapter persistence
            SelectedGameAdapter = GameAdapters.FirstOrDefault();
            SelectedZoomLevel = 1;

            if (Keyboard.IsKeyDown(Key.LeftShift))
            {
                SelectedGameAdapter = GameAdapters.FirstOrDefault(ga => ga.Adapter.PluginId == Guid.Parse("84D2BD62-7AC6-459B-B3BB-3A65855135F6")) ?? GameAdapters.First();
                SelectedZoomLevel = 2;
            }
            else if (Keyboard.IsKeyDown(Key.LeftAlt))
            {
                SelectedGameAdapter = GameAdapters.FirstOrDefault(ga => ga.Adapter.PluginId == Guid.Parse("B344166C-F1BE-49B2-9ADC-38771D0A15DA")) ?? GameAdapters.First();
                SelectedZoomLevel = 1;
            }

            SelectedEntry = Entries?.FirstOrDefault();
        }

        private void LoadEntries()
        {
            if (_gameAdapterInstance != null)
            {
                _gameAdapterInstance.Adapter.Filename = KoreFile.FilePath.FullName;
                _gameAdapterInstance.Adapter.LoadEntries(_state.Texts);
                Entries = new ObservableCollection<TextEntry>(_gameAdapterInstance.Adapter.Entries);
            }
            else
                Entries = new ObservableCollection<TextEntry>(_state.Texts);
            NotifyOfPropertyChange(() => Entries);
            NotifyOfPropertyChange(() => EntryCount);
            NotifyOfPropertyChange(() => PreviewImage);
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
                LoadEntries();
                foreach (var entry in Entries)
                    entry.Edited += (sender, args) =>
                    {
                        NotifyOfPropertyChange(() => PreviewImage);
                    };
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

        public bool AddButtonEnabled => _state is IAddEntries;

        public void AddEntry()
        {
            if (!(_state is IAddEntries add)) return;

            var entry = add.NewEntry();
            var added = false;

            if (_state is IRenameEntries ren)
            {
                var nte = new AddTextEntryViewModel
                {
                    Message = "Enter the name of the new text entry.",
                    //TODO: Implement max name length in the add dialog based on the length from the loaded text adapter
                };
                _windows.Add(nte);

                nte.ValidationCallback = () => new ValidationResult
                {
                    // TODO: Reinstate NameFilter?
                    CanClose = /*Regex.IsMatch(nte.Name, _state.NameFilter) && */_state.Texts.All(e => e.Name != nte.Name),
                    ErrorMessage = $"The '{nte.Name}' name is not valid or already exists."
                };

                if (_wm.ShowDialogAsync(nte).Result == true && add.AddEntry(entry))
                {
                    entry.Name = nte.Name;
                    added = true;
                }
            }
            else if (add.AddEntry(entry))
                added = true;

            if (added)
            {
                LoadEntries();
                foreach (var ent in Entries.Where(e => e.Name == entry.Name))
                    ent.Edited += (sender, args) =>
                    {
                        NotifyOfPropertyChange(() => PreviewImage);
                    };
                SelectedEntry = entry;
            }
        }

        public bool DeleteButtonEnabled => _state is IDeleteEntries;

        public void DeleteEntry()
        {
            if (!(_state is IDeleteEntries del)) return;

            var index = Entries.IndexOf(SelectedEntry);

            if (!del.DeleteEntry(SelectedEntry))
                MessageBox.Show("The entry could not be removed.", "Delete Failed");
            else
            {
                LoadEntries();
                if (Entries.Count > 0)
                    SelectedEntry = Entries[Math.Min(index, Entries.Count - 1)];
            }
        }

        // TODO: Make text Properties available again
        //public void EntryProperties()
        //{
        //    var prop = new Dialogs.ViewModels.PropertyEditorViewModel<TextEntry>
        //    {
        //        Message = "",
        //        Object = SelectedEntry
        //    };
        //    _windows.Add(prop);

        //    if (_wm.ShowDialogAsync(prop).Result == true)
        //    {
        //        // Cool
        //    }
        //}

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
        //        //    _state.Entries[i].EditedText = entries[i].EditedText;
        //        //}

        //        // settle...
        //        foreach (var entry in _gameAdapterInstance.Adapter.SaveEntries())
        //            _state.Entries.First(e => e.Name == entry.Name).EditedText = entry.EditedText;

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

        public override Task TryCloseAsync(bool? dialogResult = null)
        {
            for (var i = _windows.Count - 1; i >= 0; i--)
            {
                var scr = _windows[i];
                scr.TryCloseAsync(dialogResult);
                _windows.Remove(scr);
            }
            return base.TryCloseAsync(dialogResult);
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
