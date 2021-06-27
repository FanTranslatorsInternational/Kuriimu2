﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using Kanvas;
using Kontract.Interfaces.Plugins.State;
using Kontract.Kanvas;
using Kontract.Models.Image;
using Kore.Managers.Plugins;
using Kore.Progress;
using Kuriimu2.Wpf.Dialogs.Common;
using Kuriimu2.Wpf.Tools;

namespace Kuriimu2.Wpf.Dialogs.ViewModels
{
    // TODO: Revise encoding selection and process
    public sealed class EncodeImageViewModel : Screen
    {
        private readonly PluginManager _pluginManager;
        private readonly IImageState _state;
        private readonly KanvasImage _kanvasImage;

        private IEncodingInfo _selectedEncoding;
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

        public EncodeImageViewModel(PluginManager pluginManager, IImageState state, ImageInfo imageInfo)
        {
            _pluginManager = pluginManager;
            _state = state;
            _kanvasImage = new KanvasImage(state, imageInfo);

            SelectedEncoding = state.SupportedEncodings[_kanvasImage.ImageFormat];
            SourceImage = _kanvasImage.GetImage().ToBitmapImage();
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

        public IEncodingInfo SelectedEncoding
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
            //var report = new ConcurrentProgress();
            //report.ProgressChanged += Report_ProgressChanged;
            ControlsEnabled = false;

            try
            {
                //_kanvasImage.TranscodeImage();

                //ImageTranscodeResult result;

                //if (_state is IIndexedImageAdapter indexed && _selectedEncoding.IsIndexed)
                //{
                //    var ibi = _kanvasImage as IndexedImageInfo;
                //    result = await indexed.TranscodeImage(_kanvasImage, _selectedEncoding, ibi.PaletteEncoding, report);
                //}
                //else
                //{
                //    result = await _state.TranscodeImage(_kanvasImage, _selectedEncoding, report);
                //}

                //if (result.Exception != null)
                //    throw result.Exception;

                //OutputImage = result.Image.ToBitmapImage(true);
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


                    TryCloseAsync(true && ControlsEnabled);
                }
                else
                {
                    Error = results.ErrorMessage;
                    NotifyOfPropertyChange(() => Error);
                }
            }
            else
            {
                TryCloseAsync(true && ControlsEnabled);
            }
        }
    }
}
