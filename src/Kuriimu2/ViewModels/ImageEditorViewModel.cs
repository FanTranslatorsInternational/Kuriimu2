using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using Caliburn.Micro;
using Kontract.Interfaces;
using Kore;
using Kuriimu2.Dialogs.ViewModels;
using Kuriimu2.Interface;
using Kuriimu2.Tools;

namespace Kuriimu2.ViewModels
{
    public sealed class ImageEditorViewModel : Screen, IFileEditor
    {
        private IWindowManager _wm = new WindowManager();
        private List<IScreen> _windows = new List<IScreen>();
        private IImageAdapter _adapter;

        private BitmapInfo _selectedBitmap;
        private ImageSource _selectedTexture;

        public KoreFileInfo KoreFile { get; }
        public ObservableCollection<BitmapInfo> Bitmaps { get; private set; }

        public BitmapInfo SelectedBitmap
        {
            get => _selectedBitmap;
            set
            {
                _selectedBitmap = value;
                SelectedTexture = _selectedBitmap.Bitmap.ToBitmapImage();
                NotifyOfPropertyChange(() => SelectedBitmap);
            }
        }

        public override string DisplayName => KoreFile?.DisplayName;

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

        public string ImageCount => Bitmaps.Count + (Bitmaps.Count > 1 ? " Bitmaps" : " Bitmap");

        // Constructor
        public ImageEditorViewModel(KoreFileInfo koreFile)
        {
            KoreFile = koreFile;

            _adapter = KoreFile.Adapter as IImageAdapter;

            if (_adapter != null)
                Bitmaps = new ObservableCollection<BitmapInfo>(_adapter.Bitmaps);

            SelectedBitmap = Bitmaps.First();
        }

        public void ImageProperties()
        {
            if (!(_adapter is IImageAdapter fnt)) return;

            var pe = new PropertyEditorViewModel<IImageAdapter>
            {
                Title = $"Image Properties",
                Message = "Properties:",
                Object = _adapter
            };
            _windows.Add(pe);

            if (_wm.ShowDialog(pe) == true)
            {
                KoreFile.HasChanges = true;
                NotifyOfPropertyChange(() => DisplayName);
            }
        }

        #region Bitmap Management

        //public bool AddEnabled => _adapter is IAddBitmaps;

        public void AddBitmap()
        {
            //if (!(_adapter is IAddBitmaps add)) return;
        }

        public bool EditEnabled => SelectedBitmap != null;

        public void EditBitmap()
        {
            if (!(_adapter is IImageAdapter fnt)) return;

            
        }

        //public bool DeleteEnabled => _adapter is IDeleteBitmaps && SelectedBitmap != null;

        public void DeleteBitmap()
        {
            //if (!(_adapter is IDeleteBitmaps del)) return;

            //if (MessageBox.Show($"Are you sure you want to delete '{(char)SelectedBitmap.Bitmap}'?", "Delete?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            //{
            //    if (del.DeleteBitmap(SelectedBitmap))
            //    {
            //        Bitmaps = new ObservableCollection<ImageBitmap>(_adapter.Bitmaps);
            //        SelectedBitmap = Bitmaps.FirstOrDefault();
            //        NotifyOfPropertyChange(() => Bitmaps);
            //    }
            //    else
            //    {
            //        // Bitmap was not removed.
            //    }
            //}
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
