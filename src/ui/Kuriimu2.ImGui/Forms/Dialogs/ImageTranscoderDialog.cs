using System;
using System.IO;
using System.Threading.Tasks;
using ImGui.Forms.Modals;
using ImGui.Forms.Modals.IO;
using ImGui.Forms.Modals.IO.Windows;
using ImGui.Forms.Resources;
using Kanvas.Contract.Configuration;
using Kanvas.Contract.Quantization.ColorQuantizer;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Plugin.File.Image;
using Kuriimu2.ImGui.Resources;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    partial class ImageTranscoderDialog
    {
        private string? _filePath;
        private Image<Rgba32>? _origImage;
        private ImageFileInfo? _transcodedFileInfo;
        private ImageFile? _transcodedImage;

        public ImageTranscoderDialog()
        {
            InitializeComponent();

            _openBtn.Clicked += _openBtn_Clicked;

            _exportBtn.Clicked += _exportBtn_Clicked;

            _formats.SelectedItemChanged += _formats_SelectedItemChanged;
            _paletteFormats.SelectedItemChanged += _paletteFormats_SelectedItemChanged;

            _quantizers.SelectedItemChanged += _quantizers_SelectedItemChanged;
            _caches.SelectedItemChanged += _caches_SelectedItemChanged;
            _ditherers.SelectedItemChanged += _ditherers_SelectedItemChanged;
            _countText.TextChanged += _countText_TextChanged;

            _origImageBox.ContentZoomed += _origImageBox_ContentZoomed;
            _origImageBox.ContentMoved += _origImageBox_ContentMoved;
            _transcodedImageBox.ContentZoomed += _transcodedImageBox_ContentZoomed;
            _transcodedImageBox.ContentMoved += _transcodedImageBox_ContentMoved; ;

            DragDrop += ImageTranscoderDialog_DragDrop;

            UpdateFormInternal();
        }

        private async void _openBtn_Clicked(object? sender, EventArgs e)
        {
            string? selectedFile = await SelectFile();
            if (selectedFile is null)
                return;

            InitializeImages(selectedFile);

            UpdateImages();
            UpdateFormInternal();
        }

        private async void _exportBtn_Clicked(object? sender, EventArgs e)
        {
            if (_transcodedImage is null || _filePath is null)
                return;

            var sfd = new WindowsSaveFileDialog
            {
                Title = LocalizationResources.ImageMenuExportPng,
                InitialDirectory = Path.GetDirectoryName(_filePath),
                InitialFileName = Path.GetFileName(_filePath)
            };

            DialogResult result = await sfd.ShowAsync();
            if (result is not DialogResult.Ok)
                return;

            await _transcodedImage.GetImage().SaveAsPngAsync(sfd.Files[0]);
        }

        private void _origImageBox_ContentZoomed(object? sender, EventArgs e)
        {
            _origImageBox.CopyTransformTo(_transcodedImageBox);
        }

        private void _origImageBox_ContentMoved(object? sender, EventArgs e)
        {
            _origImageBox.CopyTransformTo(_transcodedImageBox);
        }

        private void _transcodedImageBox_ContentZoomed(object? sender, EventArgs e)
        {
            _transcodedImageBox.CopyTransformTo(_origImageBox);
        }

        private void _transcodedImageBox_ContentMoved(object? sender, EventArgs e)
        {
            _transcodedImageBox.CopyTransformTo(_origImageBox);
        }

        private void ImageTranscoderDialog_DragDrop(object? sender, Veldrid.Sdl2.DragDropEvent[] e)
        {
            InitializeImages(e[0].File);

            UpdateImages();
            UpdateFormInternal();
        }

        private void _formats_SelectedItemChanged(object? sender, EventArgs e)
        {
            if (_transcodedImage is not null)
                _transcodedImage.ImageInfo.Quantize = GetQuantizationOptions();

            _transcodedImage?.TranscodeImage(_formats.SelectedItem.Content);

            UpdateTranscodedImage();
            UpdateFormInternal();
        }

        private void _paletteFormats_SelectedItemChanged(object? sender, EventArgs e)
        {
            _transcodedImage?.TranscodePalette(_paletteFormats.SelectedItem.Content);

            UpdateTranscodedImage();
            UpdateFormInternal();
        }

        private void _quantizers_SelectedItemChanged(object? sender, EventArgs e)
        {
            if (_transcodedImage is not null)
                _transcodedImage.ImageInfo.Quantize = GetQuantizationOptions();

            _transcodedImage?.TranscodeImage(_formats.SelectedItem.Content);

            UpdateTranscodedImage();
            UpdateFormInternal();
        }

        private void _caches_SelectedItemChanged(object? sender, EventArgs e)
        {
            if (_transcodedImage is not null)
                _transcodedImage.ImageInfo.Quantize = GetQuantizationOptions();

            _transcodedImage?.TranscodeImage(_formats.SelectedItem.Content);

            UpdateTranscodedImage();
            UpdateFormInternal();
        }

        private void _ditherers_SelectedItemChanged(object? sender, EventArgs e)
        {
            if (_transcodedImage is not null)
                _transcodedImage.ImageInfo.Quantize = GetQuantizationOptions();

            _transcodedImage?.TranscodeImage(_formats.SelectedItem.Content);

            UpdateTranscodedImage();
            UpdateFormInternal();
        }

        private void _countText_TextChanged(object? sender, EventArgs e)
        {
            if (_transcodedImage is not null)
                _transcodedImage.ImageInfo.Quantize = GetQuantizationOptions();

            _transcodedImage?.TranscodeImage(_formats.SelectedItem.Content);

            UpdateTranscodedImage();
            UpdateFormInternal();
        }

        private void UpdateFormInternal()
        {
            _exportBtn.Enabled = _transcodedImage is not null;

            bool isIndexEncoding = IsSelectedIndexEncoding();

            _paletteFormats.Enabled = isIndexEncoding;

            IColorQuantizer quantizer = _quantizers.SelectedItem.Content(GetColorCount(), 1);

            _quantizers.Enabled = isIndexEncoding;
            _caches.Enabled = isIndexEncoding && !quantizer.IsColorCacheFixed;
            _ditherers.Enabled = isIndexEncoding;
            _countText.Enabled = isIndexEncoding;
        }

        private void UpdateImages()
        {
            if (_origImage is not null)
                _origImageBox.Image = ImageResource.FromImage(_origImage);

            UpdateTranscodedImage();
        }

        private void UpdateTranscodedImage()
        {
            if (_transcodedImage is not null)
                _transcodedImageBox.Image = ImageResource.FromImage(_transcodedImage.GetImage());
        }

        private void InitializeImages(string filePath)
        {
            try
            {
                _origImage = Image.Load<Rgba32>(filePath);

                _transcodedFileInfo = new ImageFileInfo
                {
                    BitDepth = GetSelectedBitDepth(),
                    ImageData = [],
                    ImageFormat = _formats.SelectedItem.Content,
                    ImageSize = new Size(0, 0),
                    PaletteBitDepth = GetSelectedPaletteBitDepth(),
                    PaletteData = IsSelectedIndexEncoding() ? [] : null,
                    PaletteFormat = _paletteFormats.SelectedItem.Content,
                    Quantize = GetQuantizationOptions()
                };
                _transcodedImage = new ImageFile(_transcodedFileInfo, _encodingDefinition);

                _transcodedImage.SetImage(_origImage);

                _filePath = filePath;

                _origImageBox.Reset();
                _transcodedImageBox.Reset();
            }
            catch { }
        }

        private CreateQuantizationDelegate? GetQuantizationOptions()
        {
            bool isIndexEncoding = IsSelectedIndexEncoding();
            if (!isIndexEncoding)
                return null;

            if (_ditherers.SelectedItem.Content is null)
                return options => options
                    .WithColorQuantizer(_quantizers.SelectedItem.Content)
                    .WithColorCache(_caches.SelectedItem.Content)
                    .WithColorCount(GetColorCount());

            return options => options
                .WithColorQuantizer(_quantizers.SelectedItem.Content)
                .WithColorCache(_caches.SelectedItem.Content)
                .WithColorDitherer(_ditherers.SelectedItem.Content)
                .WithColorCount(GetColorCount());
        }

        private int GetColorCount()
        {
            if (string.IsNullOrEmpty(_countText.Text))
                return 256;

            if (int.TryParse(_countText.Text, out int colorCount))
                return colorCount;

            return 256;
        }

        private int GetSelectedBitDepth()
        {
            return IsSelectedIndexEncoding()
                ? _encodingDefinition.GetIndexEncoding(_formats.SelectedItem.Content)!.IndexEncoding.BitDepth
                : _encodingDefinition.GetColorEncoding(_formats.SelectedItem.Content)!.BitDepth;
        }

        private int GetSelectedPaletteBitDepth()
        {
            return _encodingDefinition.GetPaletteEncoding(_paletteFormats.SelectedItem.Content)!.BitDepth;
        }

        private bool IsSelectedIndexEncoding()
        {
            return _encodingDefinition.ContainsIndexEncoding(_formats.SelectedItem.Content);
        }

        private async Task<string?> SelectFile()
        {
            var ofd = new WindowsOpenFileDialog
            {
                InitialDirectory = SettingsResources.LastDirectory,
                Filters = [new FileFilter(LocalizationResources.FilterPng, "png")]
            };

            // Show dialog and wait for result
            var result = await ofd.ShowAsync();
            if (result != DialogResult.Ok)
                return null;

            // Set last visited directory
            SettingsResources.LastDirectory = Path.GetDirectoryName(ofd.Files[0]);

            return ofd.Files[0];
        }
    }
}
