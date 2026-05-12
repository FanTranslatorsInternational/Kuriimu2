using System;
using System.IO;
using System.Threading.Tasks;
using ImGui.Forms.Modals;
using ImGui.Forms.Modals.IO;
using ImGui.Forms.Modals.IO.Windows;
using ImGui.Forms.Resources;
using Kanvas.Contract.Configuration;
using Kanvas.Contract.DataClasses;
using Kanvas.Contract.Quantization.ColorQuantizer;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Plugin.File.Image;
using Kuriimu2.ImGui.Resources;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    internal partial class ImageTranscoderDialog
    {
        private string? _filePath;
        private Image<Rgba32>? _origImage;
        private ImageFileInfo? _transcodedFileInfo;
        private ImageFile? _transcodedImage;

        public ImageTranscoderDialog()
        {
            InitializeComponent();

            _openBtn.Clicked += OpenBtn_Clicked;

            _exportBtn.Clicked += ExportBtn_Clicked;

            _formats.SelectedItemChanged += Formats_SelectedItemChanged;
            _paletteFormats.SelectedItemChanged += PaletteFormats_SelectedItemChanged;

            _quantizers.SelectedItemChanged += Quantizers_SelectedItemChanged;
            _caches.SelectedItemChanged += Caches_SelectedItemChanged;
            _ditherers.SelectedItemChanged += Ditherers_SelectedItemChanged;
            _countText.TextChanged += CountText_TextChanged;

            _origImageBox.ContentZoomed += OrigImageBox_ContentZoomed;
            _origImageBox.ContentMoved += OrigImageBox_ContentMoved;
            _transcodedImageBox.ContentZoomed += TranscodedImageBox_ContentZoomed;
            _transcodedImageBox.ContentMoved += TranscodedImageBox_ContentMoved;

            DragDrop += ImageTranscoderDialog_DragDrop;

            UpdateFormInternal();
        }

        private async void OpenBtn_Clicked(object? sender, EventArgs e)
        {
            string? selectedFile = await SelectFile();
            if (selectedFile is null)
                return;

            InitializeImages(selectedFile);

            UpdateImages();
            UpdateFormInternal();
        }

        private async void ExportBtn_Clicked(object? sender, EventArgs e)
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

        private void OrigImageBox_ContentZoomed(object? sender, EventArgs e)
        {
            _origImageBox.CopyTransformTo(_transcodedImageBox);
        }

        private void OrigImageBox_ContentMoved(object? sender, EventArgs e)
        {
            _origImageBox.CopyTransformTo(_transcodedImageBox);
        }

        private void TranscodedImageBox_ContentZoomed(object? sender, EventArgs e)
        {
            _transcodedImageBox.CopyTransformTo(_origImageBox);
        }

        private void TranscodedImageBox_ContentMoved(object? sender, EventArgs e)
        {
            _transcodedImageBox.CopyTransformTo(_origImageBox);
        }

        private void ImageTranscoderDialog_DragDrop(object? sender, string[] e)
        {
            InitializeImages(e[0]);

            UpdateImages();
            UpdateFormInternal();
        }

        private void Formats_SelectedItemChanged(object? sender, EventArgs e)
        {
            if (_formats.SelectedItem is null)
                return;

            if (_transcodedImage is not null)
                _transcodedImage.ImageInfo.Quantize = GetQuantizationOptions();

            _transcodedImage?.TranscodeImage(_formats.SelectedItem.Content);

            UpdateTranscodedImage();
            UpdateFormInternal();
        }

        private void PaletteFormats_SelectedItemChanged(object? sender, EventArgs e)
        {
            if (_paletteFormats.SelectedItem is null)
                return;

            _transcodedImage?.TranscodePalette(_paletteFormats.SelectedItem.Content);

            UpdateTranscodedImage();
            UpdateFormInternal();
        }

        private void Quantizers_SelectedItemChanged(object? sender, EventArgs e)
        {
            if (_formats.SelectedItem is null)
                return;

            if (_transcodedImage is not null)
                _transcodedImage.ImageInfo.Quantize = GetQuantizationOptions();

            _transcodedImage?.TranscodeImage(_formats.SelectedItem.Content);

            UpdateTranscodedImage();
            UpdateFormInternal();
        }

        private void Caches_SelectedItemChanged(object? sender, EventArgs e)
        {
            if (_formats.SelectedItem is null)
                return;

            if (_transcodedImage is not null)
                _transcodedImage.ImageInfo.Quantize = GetQuantizationOptions();

            _transcodedImage?.TranscodeImage(_formats.SelectedItem.Content);

            UpdateTranscodedImage();
            UpdateFormInternal();
        }

        private void Ditherers_SelectedItemChanged(object? sender, EventArgs e)
        {
            if (_formats.SelectedItem is null)
                return;

            if (_transcodedImage is not null)
                _transcodedImage.ImageInfo.Quantize = GetQuantizationOptions();

            _transcodedImage?.TranscodeImage(_formats.SelectedItem.Content);

            UpdateTranscodedImage();
            UpdateFormInternal();
        }

        private void CountText_TextChanged(object? sender, EventArgs e)
        {
            if (_formats.SelectedItem is null)
                return;

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

            var bitDepths = GetSelectedPaletteColorBitDepths();
            var isFixedCache = false;

            if (bitDepths is not null && _quantizers.SelectedItem is not null)
            {
                IColorQuantizer quantizer = _quantizers.SelectedItem.Content(GetColorCount(), 1, bitDepths.Value);
                isFixedCache = quantizer.IsColorCacheFixed;
            }

            _quantizers.Enabled = isIndexEncoding;
            _caches.Enabled = isIndexEncoding && !isFixedCache;
            _ditherers.Enabled = isIndexEncoding;
            _countText.Enabled = isIndexEncoding;
        }

        private void UpdateImages()
        {
            if (_origImage is not null)
                _origImageBox.SetImage(ImageResource.FromImage(_origImage));

            UpdateTranscodedImage();
        }

        private void UpdateTranscodedImage()
        {
            if (_transcodedImage is not null)
                _transcodedImageBox.SetImage(ImageResource.FromImage(_transcodedImage.GetImage()));
        }

        private void InitializeImages(string filePath)
        {
            if (_formats.SelectedItem is null || _paletteFormats.SelectedItem is null)
                return;

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
            catch
            {
                // ignored
            }
        }

        private CreateQuantizationDelegate? GetQuantizationOptions()
        {
            bool isIndexEncoding = IsSelectedIndexEncoding();
            if (!isIndexEncoding)
                return null;

            return BuildQuantizationOptions;
        }

        private IQuantizationConfigurationBuilder BuildQuantizationOptions(IQuantizationConfigurationBuilder builder)
        {
            if (_quantizers.SelectedItem is not null)
                builder = builder.WithColorQuantizer(_quantizers.SelectedItem.Content);

            if (_caches.SelectedItem is not null)
                builder = builder.WithColorCache(_caches.SelectedItem.Content);

            if (_ditherers.SelectedItem?.Content is not null)
                builder = builder.WithColorDitherer(_ditherers.SelectedItem.Content);

            builder.WithColorCount(GetColorCount());

            return builder;
        }

        private int GetColorCount()
        {
            if (string.IsNullOrEmpty(_countText.Text))
                return 256;

            return int.TryParse(_countText.Text, out int colorCount) ? colorCount : 256;
        }

        private int GetSelectedBitDepth()
        {
            if (_formats.SelectedItem is null)
                return -1;

            return IsSelectedIndexEncoding()
                ? _encodingDefinition.GetIndexEncoding(_formats.SelectedItem.Content)!.IndexEncoding.BitDepth
                : _encodingDefinition.GetColorEncoding(_formats.SelectedItem.Content)!.BitDepth;
        }

        private int GetSelectedPaletteBitDepth()
        {
            if (_paletteFormats.SelectedItem is null)
                return -1;

            return _encodingDefinition.GetPaletteEncoding(_paletteFormats.SelectedItem.Content)!.BitDepth;
        }

        private ColorChannelBitDepths? GetSelectedPaletteColorBitDepths()
        {
            if (_paletteFormats.SelectedItem is null)
                return null;

            return _encodingDefinition.GetPaletteEncoding(_paletteFormats.SelectedItem.Content)?.ColorChannelBitDepths;
        }

        private bool IsSelectedIndexEncoding()
        {
            if (_formats.SelectedItem is null)
                return false;

            return _encodingDefinition.ContainsIndexEncoding(_formats.SelectedItem.Content);
        }

        private static async Task<string?> SelectFile()
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
            SettingsResources.LastDirectory = Path.GetDirectoryName(ofd.Files[0]) ?? string.Empty;

            return ofd.Files[0];
        }
    }
}
