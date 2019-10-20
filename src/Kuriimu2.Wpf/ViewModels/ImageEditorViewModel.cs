using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Caliburn.Micro;
using Kontract.Interfaces.Image;
using Kontract.Models;
using Kontract.Models.Image;
using Kore;
using Kore.Files;
using Kore.Files.Models;
using Kuriimu2.Wpf.Dialogs.ViewModels;
using Kuriimu2.Wpf.Interfaces;
using Kuriimu2.Wpf.Tools;
using Kuriimu2.Wpf.ViewModels.ImageEditor;
using Microsoft.Win32;

namespace Kuriimu2.Wpf.ViewModels
{
    public sealed class ImageEditorViewModel : Screen, IFileEditor
    {
        private IWindowManager _wm = new WindowManager();
        private List<IScreen> _windows = new List<IScreen>();
        private readonly FileManager _fileManager;
        private readonly IImageAdapter _adapter;

        // Image View
        private int _zoomIndex = ZoomLevels.IndexOf(100);
        private double _selectedZoomLevel;

        // Bitmap List
        private BitmapEntry _selectedBitmapEntry;
        private ImageSource _selectedImage;
        private ImageSource _selectedPaletteImage;

        // Batch Export
        private string _statusText;
        private bool _progressActive = false;
        private string _progressActionName;
        private int _progressValue;

        // Data
        public KoreFileInfo KoreFile { get; set; }
        public ObservableCollection<BitmapEntry> Bitmaps { get; }

        // Constructor
        public ImageEditorViewModel(FileManager fileManager, KoreFileInfo koreFile)
        {
            _fileManager = fileManager;
            KoreFile = koreFile;

            _adapter = KoreFile.Adapter as IImageAdapter;

            if (_adapter?.BitmapInfos != null)
                Bitmaps = new ObservableCollection<BitmapEntry>(_adapter.BitmapInfos.Select(bi => new BitmapEntry(bi)));

            SelectedBitmap = Bitmaps?.FirstOrDefault();
            SelectedZoomLevel = 1;
        }

        #region Image View

        public int ImageBorderThickness => 1;

        public string ImageCount => (Bitmaps?.Count ?? 0) + ((Bitmaps?.Count ?? 0) != 1 ? " Images" : " Image");

        public static List<int> ZoomLevels { get; } = new List<int> { 7, 10, 15, 20, 25, 30, 50, 70, 100, 150, 200, 300, 400, 500, 600, 700, 800, 1000, 1200, 1600 };

        public double SelectedZoomLevel
        {
            get => _selectedZoomLevel;
            set
            {
                if (value == _selectedZoomLevel) return;
                _selectedZoomLevel = value;
                NotifyOfPropertyChange(() => SelectedZoomLevel);
                NotifyOfPropertyChange(() => ZoomLevel);
            }
        }

        public int ZoomIndex
        {
            get => _zoomIndex;
            set
            {
                if (value == _zoomIndex) return;
                _zoomIndex = value;
                SelectedZoomLevel = ZoomLevels[value] / 100D;
            }
        }

        public string ZoomLevel => $"Zoom: {ZoomLevels[ZoomIndex]}%";

        public void MouseWheel(MouseWheelEventArgs args)
        {
            if (args.Delta > 0) // Zoom In
                ZoomIndex += ZoomIndex == ZoomLevels.Count - 1 ? 0 : 1;
            else // Zoom Out
                ZoomIndex -= ZoomIndex == 0 ? 0 : 1;
        }

        #endregion

        #region Bitmap List

        // Image
        public BitmapEntry SelectedBitmap
        {
            get => _selectedBitmapEntry;
            set
            {
                if (value == _selectedBitmapEntry) return;
                _selectedBitmapEntry = value;
                SelectedImage = _selectedBitmapEntry?.BitmapInfo.Image.ToBitmapImage(true);
                if (_adapter is IIndexedImageAdapter indexed && _selectedBitmapEntry.BitmapInfo is IndexedBitmapInfo indexedInfo)
                {
                    var dimensions = (int)Math.Sqrt(indexedInfo.ColorCount);
                    SelectedPaletteImage = Kore.Utilities.Image.ComposeImage(indexedInfo.Palette, dimensions, dimensions).ToBitmapImage(true);
                }
                NotifyOfPropertyChange(() => SelectedBitmap);
                NotifyOfPropertyChange(() => PaletteImageVisibility);
            }
        }

        public ImageSource SelectedImage
        {
            get => _selectedImage;
            set
            {
                if (value == _selectedImage) return;
                _selectedImage = value;
                NotifyOfPropertyChange(() => SelectedImage);
            }
        }

        // Palette
        public Visibility PaletteImageVisibility => (_adapter is IIndexedImageAdapter && _selectedBitmapEntry.BitmapInfo is IndexedBitmapInfo) ? Visibility.Visible : Visibility.Hidden;

        public ImageSource SelectedPaletteImage
        {
            get => _selectedPaletteImage;
            set
            {
                if (value == _selectedPaletteImage) return;
                _selectedPaletteImage = value;
                NotifyOfPropertyChange(() => SelectedPaletteImage);
                NotifyOfPropertyChange(() => PaletteImageVisibility);
            }
        }
        
        // Actions

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
                FileName = KoreFile.StreamFileInfo.FileName + ".png",
                Filter = "Portable Network Graphics (*.png)|*.png"
            };

            if ((bool)sfd.ShowDialog())
            {
                SelectedBitmap.BitmapInfo.Image.Save(sfd.FileName, ImageFormat.Png);
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

            var result = await batchExport.Export(_fileManager, progress);
        }

        public void ChangeFormat()
        {
            if (!(_adapter is IImageAdapter img)) return;

            var ei = new EncodeImageViewModel(_fileManager, _adapter, _selectedBitmapEntry.BitmapInfo)
            {
                Title = $"Change Format",
                SelectedZoomLevel = SelectedZoomLevel
            };
            _windows.Add(ei);
            
            if (_wm.ShowDialogAsync(ei).Result != true) return;

            NotifyOfPropertyChange(() => SelectedBitmap);
            //if (ei.HasChanges)
            //    KoreFile.HasChanges = true;
        }

        // TODO: Make image Properties available again
        //public void ImageProperties()
        //{
        //    if (!(_adapter is IImageAdapter img)) return;

        //    var pe = new PropertyEditorViewModel<IImageAdapter>
        //    {
        //        Title = $"Image Properties",
        //        Message = "Properties:",
        //        Object = _adapter
        //    };
        //    _windows.Add(pe);

        //    if (_wm.ShowDialogAsync(pe).Result != true) return;
        //    KoreFile.HasChanges = true;
        //    NotifyOfPropertyChange(() => DisplayName);
        //}

        #endregion

        #region Batch Export

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

        #endregion

        #region Events

        public void FileDrop(DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files == null || !files.Any(f => f.EndsWith(".png")))
            {
                MessageBox.Show("None of the files were compatible image files. (bmp, png, gif)", "No Images", MessageBoxButton.OK);
                return;
            }

            ImportBitmap(files.FirstOrDefault(f => f.EndsWith(".png")));
        }

        public void ImportBitmap(string path)
        {
            if (path != null && File.Exists(path))
            {
                SelectedBitmap.BitmapInfo.Image = new System.Drawing.Bitmap(path);
                SelectedImage = _selectedBitmapEntry?.BitmapInfo.Image.ToBitmapImage(true);
                NotifyOfPropertyChange(() => SelectedBitmap);
            }
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
