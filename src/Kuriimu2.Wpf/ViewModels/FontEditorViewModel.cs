using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Caliburn.Micro;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Plugins.State.Font;
using Kontract.Models.Font;
using Kuriimu2.Wpf.Dialogs.Common;
using Kuriimu2.Wpf.Dialogs.ViewModels;
using Kuriimu2.Wpf.Interfaces;
using Kuriimu2.Wpf.Tools;

namespace Kuriimu2.Wpf.ViewModels
{
    public sealed class FontEditorViewModel : Screen, IFileEditor
    {
        private IWindowManager _wm = new WindowManager();
        private List<IScreen> _windows = new List<IScreen>();
        private IFontState _state;

        private CharacterInfo _selectedCharacter;
        private ImageSource _selectedCharacterTexture;

        public IStateInfo KoreFile { get; set; }
        public ObservableCollection<CharacterInfo> Characters { get; private set; }

        public CharacterInfo SelectedCharacter
        {
            get => _selectedCharacter;
            set
            {
                _selectedCharacter = value;
                SelectedCharacterImage = _state.Characters.First(x => x == value).Glyph.ToBitmapImage();

                NotifyOfPropertyChange(() => SelectedCharacter);
                NotifyOfPropertyChange(() => SelectedCharacterGlyphWidth);
                NotifyOfPropertyChange(() => SelectedCharacterGlyphHeight);
                NotifyOfPropertyChange(() => SelectedCharacterImageWidth);
                NotifyOfPropertyChange(() => SelectedCharacterImageHeight);
            }
        }

        public int SelectedCharacterGlyphWidth => SelectedCharacter?.Glyph.Size.Width ?? 0;

        public int SelectedCharacterGlyphHeight => SelectedCharacter?.Glyph.Size.Height ?? 0;

        public ImageSource SelectedCharacterImage
        {
            get => _selectedCharacterTexture;
            set
            {
                _selectedCharacterTexture = value;
                NotifyOfPropertyChange(() => SelectedCharacterImage);
            }
        }

        public int SelectedCharacterImageWidth => SelectedCharacterGlyphWidth * 10;
        public int SelectedCharacterImageHeight => SelectedCharacterGlyphHeight * 10;

        public string CharacterCount => Characters.Count + (Characters.Count == 1 ? " Character" : " Characters");

        // Constructor
        public FontEditorViewModel(IStateInfo koreFile)
        {
            KoreFile = koreFile;

            _state = KoreFile.PluginState as IFontState;

            if (_state != null)
                Characters = new ObservableCollection<CharacterInfo>(_state.Characters);

            SelectedCharacter = Characters.FirstOrDefault();
        }

        // TODO: Make Font Properties available again
        //public void FontProperties()
        //{
        //    if (!(_state is IFontAdapter2 fnt))
        //        return;

        //    var pe = new FontCharacter2PropertyEditorViewModel
        //    {
        //        Title = "Font Properties",
        //        Message = "Properties:",
        //        Object = _state
        //    };
        //    _windows.Add(pe);

        //    if (_wm.ShowDialogAsync(pe).Result == true)
        //    {
        //        KoreFile.HasChanges = true;
        //        NotifyOfPropertyChange(() => DisplayName);
        //    }
        //}

        #region Character Management

        #region Add characters

        public bool AddEnabled => _state is IAddCharacters;

        // TODO: Adding a new character in v2 should also ask for a new glyph image somehow
        public void AddCharacter()
        {
            if (!(_state is IAddCharacters add))
                return;

            // Add a new character based on the selected character
            var character = (CharacterInfo)SelectedCharacter.Clone();

            // Open editor to edit character properties
            var pe = new PropertyEditorViewModel<CharacterInfo>
            {
                Title = "Add Character",
                Mode = DialogMode.Add,
                Message = "New character attributes:",
                Object = character,
                ValidationCallback = () => new ValidationResult
                {
                    CanClose = _state.Characters.All(c => c.CodePoint != character.CodePoint),
                    ErrorMessage = $"Character '{(char)character.CodePoint}' already exists in the list."
                }
            };
            _windows.Add(pe);

            // If property editor was closed and adding the character was successful
            if (_wm.ShowDialogAsync(pe).Result == true && add.AddCharacter(character))
            {
                NotifyOfPropertyChange(() => DisplayName);
                Characters = new ObservableCollection<CharacterInfo>(_state.Characters);
                NotifyOfPropertyChange(() => Characters);
                SelectedCharacter = character;
            }
        }

        #endregion

        #region Edit characters

        public bool EditEnabled => SelectedCharacter != null;

        public void EditCharacter()
        {
            if (!(_state is IFontState fnt))
                return;

            // Clone the selected character so that changes don't propagate to the plugin
            var clonedCharacter = (CharacterInfo)SelectedCharacter.Clone();

            // Open editor to edit character properties
            var pe = new PropertyEditorViewModel<CharacterInfo>
            {
                Title = "Edit Character",
                Message = "Edit character properties:",
                Object = clonedCharacter,
                ValidationCallback = () => new ValidationResult
                {
                    CanClose = clonedCharacter.CodePoint == SelectedCharacter.CodePoint,
                    ErrorMessage = "You cannot change the character while editing."
                }
            };
            _windows.Add(pe);

            // If property editor was closed
            if (_wm.ShowDialogAsync(pe).Result == true)
            {
                NotifyOfPropertyChange(() => DisplayName);
                //clonedCharacter.CopyProperties(SelectedCharacter); TODO: This mechanism needs to be replaced.
                NotifyOfPropertyChange(() => SelectedCharacter);
                NotifyOfPropertyChange(() => SelectedCharacterGlyphWidth);
                NotifyOfPropertyChange(() => SelectedCharacterGlyphHeight);
            }
        }

        #endregion

        #region Delete Characters

        public bool DeleteEnabled => _state is IRemoveCharacters && SelectedCharacter != null;

        public void DeleteCharacter()
        {
            if (!(_state is IRemoveCharacters del))
                return;

            if (MessageBox.Show($"Are you sure you want to delete '{(char)SelectedCharacter.CodePoint}'?", "Delete?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                if (del.RemoveCharacter(SelectedCharacter))
                {
                    Characters = new ObservableCollection<CharacterInfo>(_state.Characters);
                    SelectedCharacter = Characters.FirstOrDefault();
                    NotifyOfPropertyChange(() => Characters);
                }
            }
        }

        #endregion

        public void GenerateFromCurrentSet()
        {
            var fg = _windows.FirstOrDefault(x => x is BitmapFontGeneratorViewModel) ?? new BitmapFontGeneratorViewModel
            {
                State = _state,
                Baseline = _state.Baseline,
                Characters = string.Join("", _state.Characters),
                //CanvasWidth = 512,
                //CanvasHeight = 512,
                GenerationCompleteCallback = () =>
                {
                    NotifyOfPropertyChange(() => DisplayName);
                    Characters = new ObservableCollection<CharacterInfo>(_state.Characters);
                    SelectedCharacter = Characters.FirstOrDefault();
                    NotifyOfPropertyChange(() => Characters);
                }
            };

            if (!_windows.Contains(fg))
                _windows.Add(fg);

            if (!fg.IsActive)
                _wm.ShowWindowAsync(fg);
        }

        #endregion

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
}
