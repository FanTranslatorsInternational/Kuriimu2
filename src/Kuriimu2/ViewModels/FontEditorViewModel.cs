using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Caliburn.Micro;
using Komponent.Tools;
using Kontract.Interfaces;
using Kore;
using Kuriimu2.Dialogs.Common;
using Kuriimu2.Dialogs.ViewModels;
using Kuriimu2.Interface;
using Kuriimu2.Tools;

namespace Kuriimu2.ViewModels
{
    public sealed class FontEditorViewModel : Screen, IFileEditor
    {
        private IWindowManager _wm = new WindowManager();
        private List<IScreen> _windows = new List<IScreen>();
        private IFontAdapter _adapter;

        private FontCharacter _selectedCharacter;
        private ImageSource _selectedTexture;

        public KoreFileInfo KoreFile { get; }
        public ObservableCollection<FontCharacter> Characters { get; private set; }

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
            get => SelectedCharacter?.GlyphX ?? 0;
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
            get => SelectedCharacter?.GlyphY ?? 0;
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
            get => SelectedCharacter?.GlyphWidth ?? 0;
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
            get => SelectedCharacter?.GlyphHeight ?? 0;
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

        public Thickness CursorMargin => new Thickness((SelectedCharacter?.GlyphX ?? 0) + ImageBorderThickness, (SelectedCharacter?.GlyphY ?? 0) + ImageBorderThickness, 0, 0);

        public string CharacterCount => Characters.Count + (Characters.Count > 1 ? " Characters" : " Character");

        // Constructor
        public FontEditorViewModel(KoreFileInfo koreFile)
        {
            KoreFile = koreFile;

            _adapter = KoreFile.Adapter as IFontAdapter;

            if (_adapter != null)
                Characters = new ObservableCollection<FontCharacter>(_adapter.Characters);

            SelectedCharacter = Characters.First();
        }

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
            _windows.Add(pe);

            if (_wm.ShowDialog(pe) == true && add.AddCharacter(character))
            {
                KoreFile.HasChanges = true;
                NotifyOfPropertyChange(() => DisplayName);
                Characters = new ObservableCollection<FontCharacter>(_adapter.Characters);
                NotifyOfPropertyChange(() => Characters);
                SelectedCharacter = character;
            }
        }

        public bool EditEnabled => SelectedCharacter != null;

        public void EditCharacter()
        {
            if (!(_adapter is IFontAdapter fnt)) return;

            // Clone the selected character so that changes don't propagate to the plugin
            var clonedCharacter = (FontCharacter)SelectedCharacter.Clone();

            var pe = new PropertyEditorViewModel
            {
                Title = "Edit Character",
                Message = "Edit character attributes:",
                Character = clonedCharacter,
                ValidationCallback = () => new ValidationResult
                {
                    CanClose = clonedCharacter.Character == SelectedCharacter.Character,
                    ErrorMessage = $"You cannot change the character while editing."
                }
            };
            _windows.Add(pe);

            if (_wm.ShowDialog(pe) == true)
            {
                KoreFile.HasChanges = true;
                NotifyOfPropertyChange(() => DisplayName);
                clonedCharacter.CopyProperties(SelectedCharacter);
                NotifyOfPropertyChange(() => SelectedCharacter);
                NotifyOfPropertyChange(() => SelectedCharacterGlyphX);
                NotifyOfPropertyChange(() => SelectedCharacterGlyphY);
                NotifyOfPropertyChange(() => SelectedCharacterGlyphWidth);
                NotifyOfPropertyChange(() => SelectedCharacterGlyphHeight);
                NotifyOfPropertyChange(() => CursorMargin);
            }
        }

        public bool DeleteEnabled => _adapter is IDeleteCharacters && SelectedCharacter != null;

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

        public void ExportTextures()
        {
            try
            {
                var dir = KoreFile.FileInfo.Directory.FullName;
                var name = Path.GetFileNameWithoutExtension(KoreFile.FileInfo.Name);

                for (var index = 0; index < _adapter.Textures.Count; index++)
                {
                    var texture = _adapter.Textures[index];
                    texture.Save(Path.Combine(dir, name + $"_{index:00}.png"), ImageFormat.Png);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MessageBox.Show("Textures exported successfully!", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void GenerateFromCurrentSet()
        {
            //    Typeface = new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
            var fg = _windows.FirstOrDefault(x => x is BitmapFontGeneratorViewModel) ?? new BitmapFontGeneratorViewModel
            {
                Adapter = _adapter,
                Characters = _adapter.Characters.Aggregate("", (i, o) => i += (char)o.Character),
                CanvasWidth = _adapter.Textures[_selectedCharacter.TextureID].Width,
                CanvasHeight = _adapter.Textures[_selectedCharacter.TextureID].Height,
                GenerationCompleteCallback = () =>
                {
                    Characters = new ObservableCollection<FontCharacter>(_adapter.Characters);
                    SelectedCharacter = Characters.FirstOrDefault();
                    NotifyOfPropertyChange(() => Characters);
                }

                //ValidationCallback = () => new ValidationResult
                //{
                //    CanClose = clonedCharacter.Character == SelectedCharacter.Character,
                //    ErrorMessage = $"You cannot change the character while editing."
                //}
            };

            if(!_windows.Contains(fg))
                _windows.Add(fg);

            if (!fg.IsActive)
                _wm.ShowWindow(fg);
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
}
