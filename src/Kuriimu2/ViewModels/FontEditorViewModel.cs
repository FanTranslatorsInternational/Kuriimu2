using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using Komponent.Tools;
using Kontract.Interfaces;
using Kore;
using Kuriimu2.DialogViewModels;
using Kuriimu2.Interface;
using Kuriimu2.Tools;
using FontFamily = System.Windows.Media.FontFamily;

namespace Kuriimu2.ViewModels
{
    public sealed class FontEditorViewModel : Screen, IEditor
    {
        private IWindowManager wm = new WindowManager();
        private IFontAdapter _adapter;

        public KoreFileInfo KoreFile { get; }
        public ObservableCollection<FontCharacter> Characters { get; private set; }

        private FontCharacter _selectedCharacter;
        private ImageSource _selectedTexture;

        public FontEditorViewModel(KoreFileInfo koreFile)
        {
            KoreFile = koreFile;

            _adapter = KoreFile.Adapter as IFontAdapter;

            if (_adapter != null)
                Characters = new ObservableCollection<FontCharacter>(_adapter.Characters);

            SelectedCharacter = Characters.First();
        }

        public FontCharacter SelectedCharacter
        {
            get => _selectedCharacter;
            set
            {
                _selectedCharacter = value;
                SelectedTexture = _adapter.Textures[_selectedCharacter.TextureID].ToBitmapImage();
                NotifyOfPropertyChange(() => SelectedCharacter);
                NotifyOfPropertyChange(() => SelectedCharacterGlyphX);
                NotifyOfPropertyChange(() => SelectedCharacterGlyphY);
                NotifyOfPropertyChange(() => SelectedCharacterGlyphWidth);
                NotifyOfPropertyChange(() => SelectedCharacterGlyphHeight);
                NotifyOfPropertyChange(() => CursorMargin);
            }
        }

        public override string DisplayName => KoreFile?.DisplayName;

        public int SelectedCharacterGlyphX
        {
            get => SelectedCharacter.GlyphX;
            set
            {
                KoreFile.HasChanges = SelectedCharacter.GlyphX != value;
                SelectedCharacter.GlyphX = value;
                NotifyOfPropertyChange(() => DisplayName);
                NotifyOfPropertyChange(() => CursorMargin);
            }
        }

        public int SelectedCharacterGlyphY
        {
            get => SelectedCharacter.GlyphY;
            set
            {
                KoreFile.HasChanges = SelectedCharacter.GlyphY != value;
                SelectedCharacter.GlyphY = value;
                NotifyOfPropertyChange(() => DisplayName);
                NotifyOfPropertyChange(() => CursorMargin);
            }
        }

        public int SelectedCharacterGlyphWidth
        {
            get => SelectedCharacter.GlyphWidth;
            set
            {
                KoreFile.HasChanges = SelectedCharacter.GlyphWidth != value;
                SelectedCharacter.GlyphWidth = value;
                NotifyOfPropertyChange(() => DisplayName);
                NotifyOfPropertyChange(() => SelectedCharacterGlyphWidth);
            }
        }

        public int SelectedCharacterGlyphHeight
        {
            get => SelectedCharacter.GlyphHeight;
            set
            {
                KoreFile.HasChanges = SelectedCharacter.GlyphHeight != value;
                SelectedCharacter.GlyphHeight = value;
                NotifyOfPropertyChange(() => DisplayName);
                NotifyOfPropertyChange(() => SelectedCharacterGlyphHeight);
            }
        }

        public ImageSource SelectedTexture
        {
            get => _selectedTexture;
            set
            {
                _selectedTexture = value;
                NotifyOfPropertyChange(() => SelectedTexture);
            }
        }

        public int ImageBorderThickness => 1;

        public Thickness CursorMargin => new Thickness(SelectedCharacter.GlyphX + ImageBorderThickness, SelectedCharacter.GlyphY + ImageBorderThickness, 0, 0);

        public string CharacterCount => Characters.Count + (Characters.Count > 1 ? " Characters" : " Character");

        #region Character Management

        public bool AddEnabled => _adapter is IAddCharacters;

        public void AddCharacter()
        {
            if (!(_adapter is IAddCharacters add)) return;

            // Add a new characters based on the selected character
            var character = add.NewCharacter(SelectedCharacter);
            character.Character = SelectedCharacter.Character;
            // Copy the perceived last character's X and Y positions (lazy mode)
            var last = Characters.OrderByDescending(c => c.GlyphY).ThenByDescending(c => c.GlyphX).FirstOrDefault();
            if (last != null)
            {
                character.GlyphX = last.GlyphX + last.GlyphWidth;
                character.GlyphY = last.GlyphY;
            }
            character.GlyphWidth = SelectedCharacter.GlyphWidth;
            character.GlyphHeight = SelectedCharacter.GlyphHeight;

            var pe = new PropertyEditorViewModel
            {
                Title = "Add Character",
                Mode = DialogMode.Add,
                Message = "New character attributes:",
                Character = character,
                ValidationCallback = () => new ValidationResult
                {
                    CanClose = _adapter.Characters.All(c => c.Character != character.Character),
                    ErrorMessage = $"The '{(char)character.Character}' character already exists in the list."
                }
            };

            if (wm.ShowDialog(pe) == true && add.AddCharacter(character))
            {
                KoreFile.HasChanges = true;
                NotifyOfPropertyChange(() => DisplayName);
                Characters = new ObservableCollection<FontCharacter>(_adapter.Characters);
                NotifyOfPropertyChange(() => Characters);
                SelectedCharacter = character;
            }
        }

        public void Edit(FontCharacter character)
        {
            if (!(_adapter is IFontAdapter fnt)) return;

            // Clone the selected character so that changes don't propagate to the plugin
            var clonedCharacter = (FontCharacter)character.Clone();

            var pe = new PropertyEditorViewModel
            {
                Title = "Edit Character",
                Message = "Edit character attributes:",
                Character = clonedCharacter,
                ValidationCallback = () => new ValidationResult
                {
                    CanClose = clonedCharacter.Character == character.Character,
                    ErrorMessage = $"You cannot change the character while editing."
                }
            };

            if (wm.ShowDialog(pe) == true)
            {
                KoreFile.HasChanges = true;
                NotifyOfPropertyChange(() => DisplayName);
                clonedCharacter.CopyProperties(character);
                NotifyOfPropertyChange(() => SelectedCharacter);
                NotifyOfPropertyChange(() => SelectedCharacterGlyphX);
                NotifyOfPropertyChange(() => SelectedCharacterGlyphY);
                NotifyOfPropertyChange(() => SelectedCharacterGlyphWidth);
                NotifyOfPropertyChange(() => SelectedCharacterGlyphHeight);
                NotifyOfPropertyChange(() => CursorMargin);
            }
        }

        public bool DeleteEnabled => _adapter is IDeleteCharacters;

        public void DeleteCharacter()
        {
            if (!(_adapter is IDeleteCharacters del)) return;

            if (MessageBox.Show($"Are you sure you want to delete '{(char)SelectedCharacter.Character}'?", "Delete?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                if (del.DeleteCharacter(SelectedCharacter))
                {
                    Characters = new ObservableCollection<FontCharacter>(_adapter.Characters);
                    SelectedCharacter = Characters.FirstOrDefault();
                    NotifyOfPropertyChange(() => Characters);
                }
                else
                {
                    // Character was not removed.
                }
            }
        }

        public void GenerateFromCurrentSet()
        {
            // TODO: Generation data needs to be user configurable
            var fontGen = new Kore.Generators.BitmapFontGenerator
            {
                Adapter = _adapter,
                Typeface = new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                FontSize = 32,
                GlyphHeight = 30,
                GlyphPadding = 1,
                MaxCanvasWidth = 512
            };

            var chars = Characters.Select(c => (ushort) c.Character).ToList();
            chars.Sort();
            fontGen.Generate(chars);

            // Temporary image assignment
            _adapter.Textures = fontGen.Textures.Select(t => t.ToBitmap()).ToList();
            Characters = new ObservableCollection<FontCharacter>(fontGen.Characters);
            SelectedCharacter = Characters.FirstOrDefault();
            NotifyOfPropertyChange(() => Characters);
        }

        #endregion

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
                NotifyOfPropertyChange(() => DisplayName);
            }
            catch (Exception)
            {
                // Handle on UI gracefully somehow~
            }
        }
    }
}
