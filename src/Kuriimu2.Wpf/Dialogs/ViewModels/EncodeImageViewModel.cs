using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using Kontract;
using Kontract.Interfaces;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Font;
using Kontract.Interfaces.Image;
using Kontract.Interfaces.Intermediate;
using Kontract.Interfaces.Text;
using Kontract.Models;
using Kontract.Models.Image;
using Kore.Files;
using Kuriimu2.Dialogs.Common;
using Kuriimu2.Tools;
using Microsoft.Win32;

namespace Kuriimu2.Dialogs.ViewModels
{
    public sealed class EncodeImageViewModel : Screen
    {
        private readonly FileManager _fileManager;
        private readonly IImageAdapter _adapter;
        private readonly BitmapInfo _bitmapInfo;

        private EncodingInfo _selectedEncoding;
        private bool _controlsEnabled = true;

        private ImageSource _sourceImage;
        private ImageSource _outputImage;

        // Image View
        private int _zoomIndex = ZoomLevels.IndexOf(100);
        private double _selectedZoomLevel;

        public string Title { get; set; } = "Open Type";
        public BitmapImage Icon { get; private set; }
        public string Message { get; set; } = "";
        public string Error { get; set; } = string.Empty;

        public Func<ValidationResult> ValidationCallback;

        public EncodeImageViewModel(FileManager fileManager, IImageAdapter adapter, BitmapInfo bitmapInfo)
        {
            _fileManager = fileManager;
            _adapter = adapter;
            _bitmapInfo = bitmapInfo;

            SelectedEncoding = _bitmapInfo.ImageEncoding;
            SourceImage = _bitmapInfo.Image.ToBitmapImage(true);
            OutputImage = SourceImage;
        }

        public ImageSource SourceImage
        {
            get => _sourceImage;
            set
            {
                if (value == _sourceImage) return;
                _sourceImage = value;
                NotifyOfPropertyChange(() => SourceImage);
            }
        }

        public ImageSource OutputImage
        {
            get => _outputImage;
            set
            {
                if (value == _outputImage) return;
                _outputImage = value;
                NotifyOfPropertyChange(() => OutputImage);
            }
        }

        public List<EncodingInfo> EncodingInfos => _adapter.ImageEncodingInfos.ToList();

        public EncodingInfo SelectedEncoding
        {
            get => _selectedEncoding;
            set
            {
                if (value == _selectedEncoding) return;
                _selectedEncoding = value;
                NotifyOfPropertyChange(() => SelectedEncoding);
            }
        }

        public bool ControlsEnabled
        {
            get => _controlsEnabled;
            set
            {
                _controlsEnabled = value;
                NotifyOfPropertyChange(() => ControlsEnabled);
            }
        }

        public void PreviewButton()
        {
            EncodeImage();
        }

        public async void EncodeImage()
        {
            var report = new Progress<ProgressReport>();
            //report.ProgressChanged += Report_ProgressChanged;
            ControlsEnabled = false;

            try
            {
                ImageTranscodeResult result;

                if (_adapter is IIndexedImageAdapter indexed && _selectedEncoding.IsIndexed)
                {
                    var ibi = _bitmapInfo as IndexedBitmapInfo;
                    result = await indexed.TranscodeImage(_bitmapInfo, _selectedEncoding, ibi.PaletteEncoding, report);
                }
                else
                {
                    result = await _adapter.TranscodeImage(_bitmapInfo, _selectedEncoding, report);
                }

                if (result.Exception != null)
                    throw result.Exception;

                OutputImage = result.Image.ToBitmapImage(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                ControlsEnabled = true;
            }
        }

        #region Image View

        public int ImageBorderThickness => 1;

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

        public void OKButton()
        {
            // Set output variables

            if (ValidationCallback != null)
            {
                var results = ValidationCallback();

                if (results.CanClose)
                {


                    TryClose(true && ControlsEnabled);
                }
                else
                {
                    Error = results.ErrorMessage;
                    NotifyOfPropertyChange(() => Error);
                }
            }
            else
            {
                TryClose(true && ControlsEnabled);
            }
        }
    }
}
