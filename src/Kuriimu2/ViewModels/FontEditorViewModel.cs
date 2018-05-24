using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using Kontract.Interface;
using Kore;

namespace Kuriimu2.ViewModels
{
    public sealed class FontEditorViewModel : Screen
    {
        private IFontAdapter _adapter;

        private KoreFileInfo KoreFile { get; }
        public ObservableCollection<FontCharacter> Characters { get; }

        private FontCharacter _selectedCharacter;
        private BitmapImage _selectedTexture;

        public FontEditorViewModel(KoreFileInfo koreFIle)
        {
            KoreFile = koreFIle;

            DisplayName = KoreFile.FileInfo.Name + (KoreFile.HasChanges ? "*" : string.Empty);
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

        public string CharacterCount => Characters.Count + (Characters.Count > 1 ? " Characters" : " Character");

        public void AddEntry()
        {
            //Entries.Add(new Entry($"Label {Entries.Count}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")));
            //NotifyOfPropertyChange(nameof(EntryCount));
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
