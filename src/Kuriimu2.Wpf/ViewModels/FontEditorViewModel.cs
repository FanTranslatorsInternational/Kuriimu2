using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Caliburn.Micro;
using Komponent.Tools;
using Kontract.Interfaces.Font;
using Kore.Files.Models;
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
        private IFontAdapter2 _adapter;

        private FontCharacter2 _selectedCharacter;
        private ImageSource _selectedCharacterTexture;

        public KoreFileInfo KoreFile { get; set; }
        public ObservableCollection<FontCharacter2> Characters { get; private set; }

        public FontCharacter2 SelectedCharacter
        {
            get => _selectedCharacter;
            set
            {
                _selectedCharacter = value;
                SelectedCharacterImage = _adapter.Characters.First(x => x == value).Glyph.ToBitmapImage();

                NotifyOfPropertyChange(() => SelectedCharacter);
                NotifyOfPropertyChange(() => SelectedCharacterGlyphWidth);
                NotifyOfPropertyChange(() => SelectedCharacterGlyphHeight);
                NotifyOfPropertyChange(() => SelectedCharacterImageWidth);
                NotifyOfPropertyChange(() => SelectedCharacterImageHeight);
            }
        }

        public int SelectedCharacterGlyphWidth => SelectedCharacter?.Glyph.Width ?? 0;

        public int SelectedCharacterGlyphHeight => SelectedCharacter?.Glyph.Height ?? 0;

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
        public FontEditorViewModel(KoreFileInfo koreFile)
        {
            KoreFile = koreFile;

            _adapter = KoreFile.Adapter as IFontAdapter2;

            if (_adapter != null)
                Characters = new ObservableCollection<FontCharacter2>(_adapter.Characters);

            SelectedCharacter = Characters.First();
        }

        public void FontProperties()
        {
            if (!(_adapter is IFontAdapter2 fnt))
                return;

            var pe = new PropertyEditorViewModel<IFontAdapter2>
            {
                Title = "Font Properties",
                Message = "Properties:",
                Object = _adapter
            };
            _windows.Add(pe);

            if (_wm.ShowDialogAsync(pe).Result == true)
            {
                KoreFile.HasChanges = true;
                NotifyOfPropertyChange(() => DisplayName);
            }
        }

        #region Character Management

        public bool AddEnabled => _adapter is IAddCharacters;

        // TODO: Adding a new character in v2 should also ask for a new glyph image somehow
        public void AddCharacter()
        {
            if (!(_adapter is IAddCharacters add))
                return;

            // Add a new character based on the selected character
            var character = (FontCharacter2)SelectedCharacter.Clone();

            // Open editor to edit character properties
            var pe = new PropertyEditorViewModel<FontCharacter2>
            {
                Title = "Add Character",
                Mode = DialogMode.Add,
                Message = "New character attributes:",
                Object = character,
                ValidationCallback = () => new ValidationResult
                {
                    CanClose = _adapter.Characters.All(c => c.Character != character.Character),
                    ErrorMessage = $"The '{(char)character.Character}' character already exists in the list."
                }
            };
            _windows.Add(pe);

            // If property editor was closed and adding the character was successful
            if (_wm.ShowDialogAsync(pe).Result == true && add.AddCharacter(character))
            {
                KoreFile.HasChanges = true;
                NotifyOfPropertyChange(() => DisplayName);
                Characters = new ObservableCollection<FontCharacter2>(_adapter.Characters);
                NotifyOfPropertyChange(() => Characters);
                SelectedCharacter = character;
            }
        }

        public bool EditEnabled => SelectedCharacter != null;

        public void EditCharacter()
        {
            if (!(_adapter is IFontAdapter2 fnt))
                return;

            // Clone the selected character so that changes don't propagate to the plugin
            var clonedCharacter = (FontCharacter2)SelectedCharacter.Clone();

            // Open editor to edit character properties
            var pe = new PropertyEditorViewModel<FontCharacter2>
            {
                Title = "Edit Character",
                Message = "Edit character properties:",
                Object = clonedCharacter,
                ValidationCallback = () => new ValidationResult
                {
                    CanClose = clonedCharacter.Character == SelectedCharacter.Character,
                    ErrorMessage = "You cannot change the character while editing."
                }
            };
            _windows.Add(pe);

            // If property editor was closed
            if (_wm.ShowDialogAsync(pe).Result == true)
            {
                KoreFile.HasChanges = true;
                NotifyOfPropertyChange(() => DisplayName);
                clonedCharacter.CopyProperties(SelectedCharacter);
                NotifyOfPropertyChange(() => SelectedCharacter);
                NotifyOfPropertyChange(() => SelectedCharacterGlyphWidth);
                NotifyOfPropertyChange(() => SelectedCharacterGlyphHeight);
            }
        }

        public bool DeleteEnabled => _adapter is IDeleteCharacters && SelectedCharacter != null;

        public void DeleteCharacter()
        {
            if (!(_adapter is IDeleteCharacters del))
                return;

            if (MessageBox.Show($"Are you sure you want to delete '{(char)SelectedCharacter.Character}'?", "Delete?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                if (del.DeleteCharacter(SelectedCharacter))
                {
                    Characters = new ObservableCollection<FontCharacter2>(_adapter.Characters);
                    SelectedCharacter = Characters.FirstOrDefault();
                    NotifyOfPropertyChange(() => Characters);
                }
            }
        }

        //public void ExportTextures()
        //{
        //    try
        //    {
        //        var dir = Path.GetDirectoryName(KoreFile.StreamFileInfo.FileName);
        //        var name = Path.GetFileNameWithoutExtension(KoreFile.StreamFileInfo.FileName);

        //        for (var index = 0; index < _adapter.Textures.Count; index++)
        //        {
        //            var texture = _adapter.Textures[index];
        //            texture.Save(Path.Combine(dir, name + $"_{index:00}.png"), ImageFormat.Png);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //    finally
        //    {
        //        MessageBox.Show("Textures exported successfully!", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
        //    }
        //}

        public void GenerateFromCurrentSet()
        {
            var fg = _windows.FirstOrDefault(x => x is BitmapFontGeneratorViewModel) ?? new BitmapFontGeneratorViewModel
            {
                Adapter = _adapter,
                Baseline = _adapter.Baseline,
                Characters = string.Join("", _adapter.Characters),
                CanvasWidth = 512,
                CanvasHeight = 512,
                GenerationCompleteCallback = () =>
                {
                    KoreFile.HasChanges = true;
                    NotifyOfPropertyChange(() => DisplayName);
                    Characters = new ObservableCollection<FontCharacter2>(_adapter.Characters);
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
