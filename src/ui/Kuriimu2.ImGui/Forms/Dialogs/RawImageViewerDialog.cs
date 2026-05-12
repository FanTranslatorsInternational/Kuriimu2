using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImGui.Forms.Controls.Text;
using ImGui.Forms.Modals.IO.Windows;
using ImGui.Forms.Modals;
using ImGui.Forms.Resources;
using Kanvas;
using Kanvas.Contract;
using Kanvas.Contract.Configuration;
using Kanvas.Contract.DataClasses;
using Kanvas.Contract.Encoding;
using Kanvas.Swizzle;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Plugin.File.Image;
using Kuriimu2.ImGui.Resources;
using SixLabors.ImageSharp;

namespace Kuriimu2.ImGui.Forms.Dialogs
{
    internal partial class RawImageViewerDialog
    {
        private FileStream? _fileStream;
        private ImageFile? _imageFile;

        public RawImageViewerDialog()
        {
            InitializeComponent();

            _openBtn.Clicked += OpenBtn_Clicked;

            _renderSwizzleBox.CheckChanged += RenderSwizzleBox_CheckChanged;
            _exportBtn.Clicked += ExportBtn_Clicked;

            _imageBox.ContentMoved += ImageBox_ContentMoved;
            _imageBox.ContentZoomed += ImageBox_ContentZoomed;
            _imageEditorBox.ContentMoved += ImageEditorBox_ContentMoved;
            _imageEditorBox.ContentZoomed += ImageEditorBox_ContentZoomed;
            _imageEditorBox.CoordinatesChanged += ImageBox_CoordinatesChanged;

            _widthTextBox.TextChanged += WidthTextBox_TextChanged;
            _heightTextBox.TextChanged += HeightTextBox_TextChanged;
            _offsetTextBox.TextChanged += OffsetTextBox_TextChanged;
            _paletteOffsetTextBox.TextChanged += PaletteOffsetTextBox_TextChanged;
            _formats.SelectedItemChanged += Formats_SelectedItemChanged;
            _paletteFormats.SelectedItemChanged += PaletteFormats_SelectedItemChanged;
            _componentsTextBox.TextChanged += ComponentsTextBox_TextChanged;
            _paletteComponentsTextBox.TextChanged += PaletteComponentsTextBox_TextChanged;
            _swizzles.SelectedItemChanged += Swizzles_SelectedItemChanged;
            _swizzleTextBox.TextChanged += SwizzleTextBox_TextChanged;

            DragDrop += RawImageViewerDialog_DragDrop;

            UpdateComponents();
            UpdatePaletteComponents();
            UpdateFormInternal();
        }

        protected override async Task CloseInternal()
        {
            if (_fileStream is not null)
                await _fileStream.DisposeAsync();
        }

        private async void OpenBtn_Clicked(object? sender, EventArgs e)
        {
            string? selectedFile = await SelectFile();
            if (selectedFile is null)
                return;

            if (_fileStream is not null)
                await _fileStream.DisposeAsync();

            _fileStream = File.OpenRead(selectedFile);

            UpdatePreview(true);
            UpdateFormInternal();
        }

        private void ImageBox_ContentZoomed(object? sender, EventArgs e)
        {
            _imageBox.CopyTransformTo(_imageEditorBox);
        }

        private void ImageBox_ContentMoved(object? sender, EventArgs e)
        {
            _imageBox.CopyTransformTo(_imageEditorBox);
        }

        private void ImageEditorBox_ContentZoomed(object? sender, EventArgs e)
        {
            _imageEditorBox.CopyTransformTo(_imageBox);
        }

        private void ImageEditorBox_ContentMoved(object? sender, EventArgs e)
        {
            _imageEditorBox.CopyTransformTo(_imageBox);
        }

        private void ImageBox_CoordinatesChanged(object? sender, EventArgs e)
        {
            UpdatePreview();
        }

        private void RawImageViewerDialog_DragDrop(object? sender, string[] e)
        {
            _fileStream?.Dispose();

            _fileStream = File.OpenRead(e[0]);

            UpdatePreview();
            UpdateFormInternal();
        }

        private void RenderSwizzleBox_CheckChanged(object? sender, EventArgs e)
        {
            _imageBox.RenderSwizzle = _renderSwizzleBox.Checked;
            _imageEditorBox.RenderSwizzle = _renderSwizzleBox.Checked;

            UpdatePreview();
            UpdateFormInternal();
        }

        private async void ExportBtn_Clicked(object? sender, EventArgs e)
        {
            if (_fileStream is null || _imageFile is null)
                return;

            var sfd = new WindowsSaveFileDialog
            {
                Title = LocalizationResources.ImageMenuExportPng,
                InitialDirectory = Path.GetDirectoryName(_fileStream.Name),
                InitialFileName = Path.GetFileNameWithoutExtension(_fileStream.Name) + ".png"
            };

            DialogResult result = await sfd.ShowAsync();
            if (result is not DialogResult.Ok)
                return;

            await _imageFile.GetImage().SaveAsPngAsync(sfd.Files[0]);
        }

        private void WidthTextBox_TextChanged(object? sender, EventArgs e)
        {
            UpdatePreview();
            UpdateFormInternal();
        }

        private void HeightTextBox_TextChanged(object? sender, EventArgs e)
        {
            UpdatePreview();
            UpdateFormInternal();
        }

        private void OffsetTextBox_TextChanged(object? sender, EventArgs e)
        {
            UpdatePreview();
            UpdateFormInternal();
        }

        private void PaletteOffsetTextBox_TextChanged(object? sender, EventArgs e)
        {
            UpdatePreview();
            UpdateFormInternal();
        }

        private void Formats_SelectedItemChanged(object? sender, EventArgs e)
        {
            UpdateComponents();
            UpdatePreview();

            UpdateFormInternal();
        }

        private void PaletteFormats_SelectedItemChanged(object? sender, EventArgs e)
        {
            UpdatePaletteComponents();
            UpdatePreview();

            UpdateFormInternal();
        }

        private void ComponentsTextBox_TextChanged(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_componentsTextBox.Text) || _formats.SelectedItem is null)
                return;

            _components[_formats.SelectedItem.Content] = _componentsTextBox.Text;

            UpdateFormats();
            UpdatePreview();

            UpdateFormInternal();
        }

        private void PaletteComponentsTextBox_TextChanged(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_paletteComponentsTextBox.Text) || _paletteFormats.SelectedItem is null)
                return;

            _paletteComponents[_paletteFormats.SelectedItem.Content] = _paletteComponentsTextBox.Text;

            UpdateFormats();
            UpdatePreview();

            UpdateFormInternal();
        }

        private void Swizzles_SelectedItemChanged(object? sender, EventArgs e)
        {
            UpdateSwizzle();
            UpdatePreview();

            UpdateFormInternal();
        }

        private void SwizzleTextBox_TextChanged(object? sender, EventArgs e)
        {
            UpdateSwizzle();
            UpdatePreview();

            UpdateFormInternal();
        }

        private void UpdateSwizzle()
        {
            if (_swizzles.SelectedItem is null)
                return;

            if (IsCustomSwizzle())
                return;

            try
            {
                IImageSwizzle? swizzle = CreateSwizzle(_swizzles.SelectedItem.Content);

                _imageBox.SetSwizzle(swizzle);
            }
            catch
            {
                _imageBox.SetSwizzle(null);
            }
        }

        private void UpdatePreview(bool resetZoom = false)
        {
            if (_fileStream is null || _formats.SelectedItem is null)
                return;

            int bitDepth = _encodingDefinition.ContainsColorEncoding(_formats.SelectedItem.Content)
                ? _encodingDefinition.GetColorEncoding(_formats.SelectedItem.Content)!.BitDepth
                : _encodingDefinition.GetIndexEncoding(_formats.SelectedItem.Content)!.IndexEncoding.BitDepth;
            int offset = GetValue(_offsetTextBox);
            Size imageSize = GetImageSize();

            CreatePixelRemapperDelegate? swizzleDelegate = null;
            if (_swizzles.SelectedItem?.Content is not null)
            {
                swizzleDelegate = _swizzles.SelectedItem.Content;
            }
            else if (IsCustomSwizzle())
            {
                (int, int)[] coords = GetCustomSwizzleCoordinates();
                swizzleDelegate = context => new CustomSwizzle(context, new MasterSwizzle(context.Size.Width, Point.Empty, coords));
            }

            byte[] imageData = ReadImageData(offset, imageSize, bitDepth, swizzleDelegate);

            var imageInfo = new ImageFileInfo
            {
                BitDepth = bitDepth,
                ImageData = imageData,
                ImageFormat = _formats.SelectedItem.Content,
                ImageSize = imageSize,
                RemapPixels = swizzleDelegate
            };

            if (IsSelectedIndexEncoding() && _paletteFormats.SelectedItem is not null)
            {
                IIndexEncoding indexEncoding = _encodingDefinition.GetIndexEncoding(_formats.SelectedItem.Content)!.IndexEncoding;
                int paletteBitDepth = _encodingDefinition.GetPaletteEncoding(_paletteFormats.SelectedItem.Content)!.BitDepth;
                int paletteOffset = GetValue(_paletteOffsetTextBox);

                byte[] paletteData = ReadPaletteData(paletteOffset, indexEncoding.MaxColors, paletteBitDepth);

                imageInfo.PaletteBitDepth = paletteBitDepth;
                imageInfo.PaletteData = paletteData;
                imageInfo.PaletteFormat = _paletteFormats.SelectedItem.Content;
            }

            try
            {
                _imageFile = new ImageFile(imageInfo, _encodingDefinition);

                var imageResource = ImageResource.FromImage(_imageFile.GetImage());
                _imageBox.SetImage(imageResource);
                _imageEditorBox.SetImage(imageResource);

                if (resetZoom)
                {
                    _imageBox.Reset();
                    _imageEditorBox.Reset();
                }
            }
            catch
            {
                _imageFile = null;
                _imageBox.SetImage(null);
                _imageEditorBox.SetImage(null);
            }
        }

        private void UpdateFormInternal()
        {
            _exportBtn.Enabled = _fileStream is not null && _imageFile is not null;

            bool isIndexEncoding = IsSelectedIndexEncoding();

            _paletteOffsetTextBox.Enabled = isIndexEncoding;
            _paletteFormats.Enabled = isIndexEncoding;

            _componentsTextBox.Enabled = _formats.SelectedItem is not null && _components.ContainsKey(_formats.SelectedItem.Content);
            _paletteComponentsTextBox.Enabled = isIndexEncoding && _paletteFormats.SelectedItem is not null && _paletteComponents.ContainsKey(_paletteFormats.SelectedItem.Content);

            bool hasSwizzleParameter = _swizzles.SelectedItem is not null && _swizzleParameterItems.Contains(_swizzles.SelectedItem);
            bool isCustomSizzle = IsCustomSwizzle();

            _renderSwizzleBox.Enabled = _swizzles.SelectedItem?.Content is not null || isCustomSizzle;

            _swizzleTextBox.Enabled = hasSwizzleParameter;
            _mainLayout.Items[1] = isCustomSizzle ? _imageEditorBox : _imageBox;
        }

        private void UpdateComponents()
        {
            if (_formats.SelectedItem is null)
                return;

            if (!_components.TryGetValue(_formats.SelectedItem.Content, out string? components))
                return;

            _componentsTextBox.Text = components;
        }

        private void UpdatePaletteComponents()
        {
            if (_paletteFormats.SelectedItem is null)
                return;

            if (!_paletteComponents.TryGetValue(_paletteFormats.SelectedItem.Content, out string? components))
                return;

            _paletteComponentsTextBox.Text = components;
        }

        private bool IsSelectedIndexEncoding()
        {
            return _formats.SelectedItem is not null && _encodingDefinition.ContainsIndexEncoding(_formats.SelectedItem.Content);
        }

        private bool IsCustomSwizzle()
        {
            return _swizzles.SelectedItem == _customSwizzleItem;
        }

        private (int, int)[] GetCustomSwizzleCoordinates()
        {
            return [.. _imageEditorBox.Coordinates.Select(x => ((int)x.X, (int)x.Y))];
        }

        private Size GetImageSize()
        {
            int width = GetValue(_widthTextBox);
            int height = GetValue(_heightTextBox);

            return new Size(width, height);
        }

        private static int GetValue(TextBox textBox)
        {
            if (string.IsNullOrEmpty(textBox.Text))
                return 0;

            int dimension;

            if (!textBox.Text.StartsWith("0x", StringComparison.Ordinal))
                return int.TryParse(textBox.Text, out dimension) ? dimension : 0;

            if (textBox.Text.Length <= 2)
                return 0;

            return int.TryParse(textBox.Text[2..], NumberStyles.HexNumber, null, out dimension) ? dimension : 0;
        }

        private byte[] ReadImageData(int offset, Size size, int bitDepth, CreatePixelRemapperDelegate? swizzleDelegate)
        {
            int dataLength = size.Width * size.Height * ((bitDepth + 7) & ~7) / 8;

            if (_fileStream is null || offset >= _fileStream.Length)
                return new byte[dataLength];

            IImageSwizzle? swizzle = CreateSwizzle(swizzleDelegate);
            if (swizzle is not null)
            {
                size = new Size(SizePadding.Multiple(size.Width, swizzle.MacroTileWidth), SizePadding.Multiple(size.Height, swizzle.MacroTileHeight));
                dataLength = size.Width * size.Height * ((bitDepth + 7) & ~7) / 8;
            }

            if (offset + dataLength > _fileStream.Length)
                return new byte[dataLength];

            _fileStream.Position = offset;

            var buffer = new byte[dataLength];
            _ = _fileStream.Read(buffer);

            return buffer;
        }

        private byte[] ReadPaletteData(int offset, int colorCount, int bitDepth)
        {
            if (_fileStream is null || offset >= _fileStream.Length)
                return [];

            int dataLength = colorCount * ((bitDepth + 7) & ~7) / 8;
            dataLength = (int)Math.Min(dataLength, _fileStream.Length - offset);

            _fileStream.Position = offset;

            var buffer = new byte[dataLength];
            _ = _fileStream.Read(buffer);

            return buffer;
        }

        private IImageSwizzle? CreateSwizzle(CreatePixelRemapperDelegate? swizzleDelegate)
        {
            if (_formats.SelectedItem is null)
                return null;

            IEncodingInfo encoding = _encodingDefinition.ContainsColorEncoding(_formats.SelectedItem.Content)
                ? _encodingDefinition.GetColorEncoding(_formats.SelectedItem.Content)!
                : _encodingDefinition.GetIndexEncoding(_formats.SelectedItem.Content)!.IndexEncoding;
            var options = new SwizzleOptions { EncodingInfo = encoding, Size = GetImageSize() };
            return swizzleDelegate?.Invoke(options);
        }

        private static async Task<string?> SelectFile()
        {
            var ofd = new WindowsOpenFileDialog { InitialDirectory = SettingsResources.LastDirectory };

            // Show dialog and wait for result
            var result = await ofd.ShowAsync();
            if (result != DialogResult.Ok)
                return null;

            // Set last visited directory
            SettingsResources.LastDirectory = Path.GetDirectoryName(ofd.Files[0]) ?? string.Empty;

            return ofd.Files[0];
        }
    }

    internal class CustomSwizzle(SwizzleOptions options, MasterSwizzle swizzle) : IImageSwizzle
    {
        /// <inheritdoc />
        public int Width { get; } = options.Size.Width;

        /// <inheritdoc />
        public int Height { get; } = options.Size.Height;

        /// <inheritdoc />
        public int MacroTileWidth => swizzle.MacroTileWidth;

        /// <inheritdoc />
        public int MacroTileHeight => swizzle.MacroTileHeight;

        /// <inheritdoc />
        public Point Transform(Point point) => Get(point.Y * Width + point.X);

        /// <inheritdoc />
        public Point Get(int pointCount) => swizzle.Get(pointCount);
    }
}
