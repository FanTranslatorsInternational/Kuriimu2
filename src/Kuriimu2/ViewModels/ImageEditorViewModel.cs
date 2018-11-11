using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using Kontract;
using Kontract.Interfaces;
using Kore;
using Kuriimu2.Dialogs.ViewModels;
using Kuriimu2.Interfaces;
using Kuriimu2.Tools;
using Microsoft.Win32;

namespace Kuriimu2.ViewModels
{
    public sealed class ImageEditorViewModel : Screen, IFileEditor
    {
        private IWindowManager _wm = new WindowManager();
        private List<IScreen> _windows = new List<IScreen>();
        private readonly Kore.Kore _kore;
        private readonly IImageAdapter _adapter;

        private BitmapEntry _selectedBitmapInfo;
        private ImageSource _selectedTexture;
        private string _statusText;
        private bool _progressActive;
        private string _progressActionName;
        private int _progressValue;

        public KoreFileInfo KoreFile { get; }
        public ObservableCollection<BitmapEntry> Bitmaps { get; }

        public BitmapEntry SelectedBitmap
        {
            get => _selectedBitmapInfo;
            set
            {
                if (value == _selectedBitmapInfo) return;
                _selectedBitmapInfo = value;
                SelectedTexture = _selectedBitmapInfo?.BitmapInfo.Bitmaps.FirstOrDefault()?.ToBitmapImage();
                NotifyOfPropertyChange(() => SelectedBitmap);
            }
        }

        public ImageSource SelectedTexture
        {
            get => _selectedTexture;
            set
            {
                if (value == _selectedTexture) return;
                _selectedTexture = value;
                NotifyOfPropertyChange(() => SelectedTexture);
            }
        }

        public string StatusText
        {
            get => _statusText;
            set
            {
                if (value == _statusText) return;
                _statusText = value;
                NotifyOfPropertyChange(() => StatusText);
            }
        }

        public string ProgressActionName
        {
            get => _progressActionName;
            set
            {
                if (value == _progressActionName) return;
                _progressActionName = value;
                NotifyOfPropertyChange(() => ProgressActionName);
            }
        }

        public bool ProgressActive
        {
            get => _progressActive;
            set
            {
                if (value == _progressActive) return;
                _progressActive = value;
                NotifyOfPropertyChange(() => ProgressActive);
            }
        }

        public int ProgressValue
        {
            get => _progressValue;
            set
            {
                if (value == _progressValue) return;
                _progressValue = value;
                NotifyOfPropertyChange(() => ProgressValue);
            }
        }

        public int ImageBorderThickness => 1;

        public string ImageCount => (Bitmaps?.Count ?? 0) + ((Bitmaps?.Count ?? 0) != 1 ? " Bitmaps" : " Bitmap");

        // Constructor
        public ImageEditorViewModel(Kore.Kore kore, KoreFileInfo koreFile)
        {
            _kore = kore;
            KoreFile = koreFile;

            _adapter = KoreFile.Adapter as IImageAdapter;

            if (_adapter?.BitmapInfos != null)
                Bitmaps = new ObservableCollection<BitmapEntry>(_adapter.BitmapInfos.Select(bi => new BitmapEntry(bi)));

            SelectedBitmap = Bitmaps?.FirstOrDefault();
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

            if (_wm.ShowDialog(pe) != true) return;
            KoreFile.HasChanges = true;
            NotifyOfPropertyChange(() => DisplayName);
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
            if (!(_adapter is IImageAdapter img)) return;
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

        public void ExportPng()
        {
            if (!(_adapter is IImageAdapter img)) return;

            var sfd = new SaveFileDialog
            {
                Title = "Export PNG",
                FileName = KoreFile.FileInfo.Name + ".png",
                Filter = "Portable Network Graphics (*.png)|*.png"
            };

            if ((bool)sfd.ShowDialog())
            {
                SelectedBitmap.BitmapInfo.Bitmaps.First().Save(sfd.FileName, ImageFormat.Png);
            }
        }

        public async Task BatchExportPng()
        {
            StatusText = "Starting batch export...";

            var ofd = new OpenFileDialog();
            if (!(bool)ofd.ShowDialog()) return;

            var batchExport = new Kore.Batch.BatchExport<IImageAdapter> { InputDirectory = Path.GetDirectoryName(ofd.FileName) };
            ProgressActionName = "Batch Export PNG";

            var progress = new Progress<ProgressReport>(p =>
            {
                ProgressValue = (int)Math.Min(p.Percentage * 10, 1000);
                if (p.HasMessage)
                {
                    var lines = StatusText.Split('\n');
                    if (lines.Length == 10)
                        StatusText = string.Join("\n", lines.Skip(1).Take(10)) + "\r\n" + p.Message;
                    else
                        StatusText += "\r\n" + p.Message;
                }

                if (p.Data == null) return;
                var (current, max) = ((int current, int max))p.Data;
                ProgressActionName = $"Batch Export PNG ({current} / {max})";
            });

            var result = await batchExport.Export(_kore, progress);
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

    public sealed class BitmapEntry
    {
        public BitmapInfo BitmapInfo = null;

        public string Name => BitmapInfo?.Name;

        public BitmapImage ImageOne => BitmapInfo?.Bitmaps.FirstOrDefault()?.ToBitmapImage();

        public BitmapImage ImageTwo => BitmapInfo?.Bitmaps.Skip(1).FirstOrDefault()?.ToBitmapImage();
        public Visibility ImageTwoVisible => BitmapInfo?.Bitmaps.Count > 1 ? Visibility.Visible : Visibility.Hidden;

        public BitmapImage ImageThree => BitmapInfo?.Bitmaps.Skip(2).FirstOrDefault()?.ToBitmapImage();
        public Visibility ImageThreeVisible => BitmapInfo?.Bitmaps.Count > 2 ? Visibility.Visible : Visibility.Hidden;

        public BitmapImage ImageFour => BitmapInfo?.Bitmaps.Skip(3).FirstOrDefault()?.ToBitmapImage();
        public Visibility ImageFourVisible => BitmapInfo?.Bitmaps.Count > 3 ? Visibility.Visible : Visibility.Hidden;

        public BitmapImage ImageFive => BitmapInfo?.Bitmaps.Skip(4).FirstOrDefault()?.ToBitmapImage();
        public Visibility ImageFiveVisible => BitmapInfo?.Bitmaps.Count > 4 ? Visibility.Visible : Visibility.Hidden;

        public BitmapImage ImageSix => BitmapInfo?.Bitmaps.Skip(5).FirstOrDefault()?.ToBitmapImage();
        public Visibility ImageSixVisible => BitmapInfo?.Bitmaps.Count > 5 ? Visibility.Visible : Visibility.Hidden;

        public BitmapImage ImageSeven => BitmapInfo?.Bitmaps.Skip(6).FirstOrDefault()?.ToBitmapImage();
        public Visibility ImageSevenVisible => BitmapInfo?.Bitmaps.Count > 6 ? Visibility.Visible : Visibility.Hidden;

        public BitmapEntry(BitmapInfo bitmapInfo)
        {
            BitmapInfo = bitmapInfo;
        }
    }
}
