using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using Komponent.Tools;
using Kontract.Interfaces;
using Kore;
using Kuriimu2.DialogViewModels;
using Kuriimu2.Interface;

namespace Kuriimu2.ViewModels
{
    public sealed class FontEditorViewModel : Screen, IEditor
    {
        private IFontAdapter _adapter;

        public KoreFileInfo KoreFile { get; }
        public ObservableCollection<FontCharacter> Characters { get; private set; }

        private FontCharacter _selectedCharacter;
        private BitmapImage _selectedTexture;

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
                SelectedTexture = BitmapToImageSource(_adapter.Textures[_selectedCharacter.TextureID]);
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

        public BitmapImage SelectedTexture
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

            var last = Characters.OrderByDescending(c => c.GlyphY).ThenByDescending(c => c.GlyphX).FirstOrDefault();

            var character = add.NewCharacter(SelectedCharacter);
            character.Character = SelectedCharacter.Character;
            if (last != null)
            {
                character.GlyphX = last.GlyphX + last.GlyphWidth;
                character.GlyphY = last.GlyphY;
            }
            character.GlyphWidth = SelectedCharacter.GlyphWidth;
            character.GlyphHeight = SelectedCharacter.GlyphHeight;

            var feac = new PropertyEditorViewModel
            {
                Title = "Add Character",
                Mode = DialogMode.Add,
                Message = "New character attributes:",
                Character = character,
                ValidationCallback = () => new ValidationResult
                {
                    CanClose = _adapter.Characters.All(c => c.Character != character.Character),
                    ErrorMessage = $"The '{(char) character.Character}' character already exists in the list."
                }
            };

            IWindowManager wm = new WindowManager();
            if (wm.ShowDialog(feac) == true && add.AddCharacter(character))
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

            var clonedCharacter = (FontCharacter)character.Clone();
            
            var feac = new PropertyEditorViewModel
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

            IWindowManager wm = new WindowManager();
            if (wm.ShowDialog(feac) == true)
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

        private static BitmapImage BitmapToImageSource(Image bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }
    }
}
