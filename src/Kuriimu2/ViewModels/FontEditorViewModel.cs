using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using Kontract.Interface;
using Kore;

namespace Kuriimu2.ViewModels
{
    public sealed class FontEditorViewModel : Screen
    {
        private IFontAdapter _adapter;

        public KoreFileInfo KoreFile { get; }
        public ObservableCollection<FontCharacter> Characters { get; }

        private FontCharacter _selectedCharacter;
        private BitmapImage _selectedTexture;

        public FontEditorViewModel(KoreFileInfo koreFile)
        {
            KoreFile = koreFile;

            DisplayName = KoreFile.DisplayName;
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
                SelectedTexture = BitmapToImageSource(_adapter.Textures[_selectedCharacter.TextureIndex]);
                NotifyOfPropertyChange(() => SelectedCharacter);
                NotifyOfPropertyChange(() => SelectedCharacterGlyphX);
                NotifyOfPropertyChange(() => SelectedCharacterGlyphY);
                NotifyOfPropertyChange(() => SelectedCharacterGlyphWidth);
                NotifyOfPropertyChange(() => SelectedCharacterGlyphHeight);
                NotifyOfPropertyChange(() => CursorMargin);
            }
        }

        public int SelectedCharacterGlyphX
        {
            get => SelectedCharacter.GlyphX;
            set
            {
                SelectedCharacter.GlyphX = value;
                NotifyOfPropertyChange(() => CursorMargin);
            }
        }

        public int SelectedCharacterGlyphY
        {
            get => SelectedCharacter.GlyphY;
            set
            {
                SelectedCharacter.GlyphY = value;
                NotifyOfPropertyChange(() => CursorMargin);
            }
        }

        public int SelectedCharacterGlyphWidth
        {
            get => SelectedCharacter.GlyphWidth;
            set
            {
                SelectedCharacter.GlyphWidth = value;
                NotifyOfPropertyChange(() => SelectedCharacterGlyphWidth);
            }
        }

        public int SelectedCharacterGlyphHeight
        {
            get => SelectedCharacter.GlyphHeight;
            set
            {
                SelectedCharacter.GlyphHeight = value;
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

        public Thickness CursorMargin => new Thickness(SelectedCharacter.GlyphX, SelectedCharacter.GlyphY, 0, 0);

        public string CharacterCount => Characters.Count + (Characters.Count > 1 ? " Characters" : " Character");

        #region Character Management

        public bool AddEnabled => _adapter is IAddCharacters;

        public void AddCharacter()
        {
            if (!(_adapter is IAddCharacters)) return;

            (_adapter as IAddCharacters).AddCharacter(new FontCharacter
            {
                Character = 'b',

            });
            NotifyOfPropertyChange(() => Characters);
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
                NotifyOfPropertyChange(DisplayName);
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
