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
    partial class RawImageViewerDialog
    {
        private FileStream? _fileStream;
        private ImageFile? _imageFile;

        public RawImageViewerDialog()
        {
            InitializeComponent();

            _openBtn.Clicked += _openBtn_Clicked;

            _renderSwizzleBox.CheckChanged += _renderSwizzleBox_CheckChanged;
            _exportBtn.Clicked += _exportBtn_Clicked;

            _imageBox.ContentMoved += _imageBox_ContentMoved;
            _imageBox.ContentZoomed += _imageBox_ContentZoomed;
            _imageEditorBox.ContentMoved += _imageEditorBox_ContentMoved;
            _imageEditorBox.ContentZoomed += _imageEditorBox_ContentZoomed;
            _imageEditorBox.CoordinatesChanged += _imageBox_CoordinatesChanged;

            _widthTextBox.TextChanged += _widthTextBox_TextChanged;
            _heightTextBox.TextChanged += _heightTextBox_TextChanged;
            _offsetTextBox.TextChanged += _offsetTextBox_TextChanged;
            _paletteOffsetTextBox.TextChanged += _paletteOffsetTextBox_TextChanged;
            _formats.SelectedItemChanged += _formats_SelectedItemChanged;
            _paletteFormats.SelectedItemChanged += _paletteFormats_SelectedItemChanged;
            _componentsTextBox.TextChanged += _componentsTextBox_TextChanged;
            _paletteComponentsTextBox.TextChanged += _paletteComponentsTextBox_TextChanged;
            _swizzles.SelectedItemChanged += _swizzles_SelectedItemChanged;
            _swizzleTextBox.TextChanged += _swizzleTextBox_TextChanged;

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

        private async void _openBtn_Clicked(object? sender, EventArgs e)
        {
            string? selectedFile = await SelectFile();
            if (selectedFile is null)
                return;

            _fileStream?.Dispose();

            _fileStream = File.OpenRead(selectedFile);

            UpdatePreview();
            UpdateFormInternal();
        }

        private void _imageBox_ContentZoomed(object? sender, EventArgs e)
        {
            _imageBox.CopyTransformTo(_imageEditorBox);
        }

        private void _imageBox_ContentMoved(object? sender, EventArgs e)
        {
            _imageBox.CopyTransformTo(_imageEditorBox);
        }

        private void _imageEditorBox_ContentZoomed(object? sender, EventArgs e)
        {
            _imageEditorBox.CopyTransformTo(_imageBox);
        }

        private void _imageEditorBox_ContentMoved(object? sender, EventArgs e)
        {
            _imageEditorBox.CopyTransformTo(_imageBox);
        }

        private void _imageBox_CoordinatesChanged(object? sender, EventArgs e)
        {
            UpdatePreview();
        }

        private void RawImageViewerDialog_DragDrop(object? sender, Veldrid.Sdl2.DragDropEvent[] e)
        {
            _fileStream?.Dispose();

            _fileStream = File.OpenRead(e[0].File);

            UpdatePreview();
            UpdateFormInternal();
        }

        private void _renderSwizzleBox_CheckChanged(object? sender, EventArgs e)
        {
            _imageBox.RenderSwizzle = _renderSwizzleBox.Checked;
            _imageEditorBox.RenderSwizzle = _renderSwizzleBox.Checked;

            UpdatePreview();
            UpdateFormInternal();
        }

        private async void _exportBtn_Clicked(object? sender, EventArgs e)
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

        private void _widthTextBox_TextChanged(object? sender, System.EventArgs e)
        {
            UpdatePreview();
            UpdateFormInternal();
        }

        private void _heightTextBox_TextChanged(object? sender, System.EventArgs e)
        {
            UpdatePreview();
            UpdateFormInternal();
        }

        private void _offsetTextBox_TextChanged(object? sender, System.EventArgs e)
        {
            UpdatePreview();
            UpdateFormInternal();
        }

        private void _paletteOffsetTextBox_TextChanged(object? sender, System.EventArgs e)
        {
            UpdatePreview();
            UpdateFormInternal();
        }

        private void _formats_SelectedItemChanged(object? sender, System.EventArgs e)
        {
            UpdateComponents();
            UpdatePreview();

            UpdateFormInternal();
        }

        private void _paletteFormats_SelectedItemChanged(object? sender, System.EventArgs e)
        {
            UpdatePaletteComponents();
            UpdatePreview();

            UpdateFormInternal();
        }

        private void _componentsTextBox_TextChanged(object? sender, System.EventArgs e)
        {
            if (string.IsNullOrEmpty(_componentsTextBox.Text))
                return;

            _components[_formats.SelectedItem.Content] = _componentsTextBox.Text;

            UpdateFormats();
            UpdatePreview();

            UpdateFormInternal();
        }

        private void _paletteComponentsTextBox_TextChanged(object? sender, System.EventArgs e)
        {
            if (string.IsNullOrEmpty(_paletteComponentsTextBox.Text))
                return;

            _paletteComponents[_paletteFormats.SelectedItem.Content] = _paletteComponentsTextBox.Text;

            UpdateFormats();
            UpdatePreview();

            UpdateFormInternal();
        }

        private void _swizzles_SelectedItemChanged(object? sender, EventArgs e)
        {
            UpdateSwizzle();
            UpdatePreview();

            UpdateFormInternal();
        }

        private void _swizzleTextBox_TextChanged(object? sender, EventArgs e)
        {
            UpdateSwizzle();
            UpdatePreview();

            UpdateFormInternal();
        }

        private void UpdateSwizzle()
        {
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

        private void UpdatePreview()
        {
            if (_fileStream is null)
                return;

            int bitDepth = _encodingDefinition.ContainsColorEncoding(_formats.SelectedItem.Content)
                ? _encodingDefinition.GetColorEncoding(_formats.SelectedItem.Content)!.BitDepth
                : _encodingDefinition.GetIndexEncoding(_formats.SelectedItem.Content)!.IndexEncoding.BitDepth;
            int offset = GetValue(_offsetTextBox);
            Size imageSize = GetImageSize();

            CreatePixelRemapperDelegate? swizzleDelegate = null;
            if (_swizzles.SelectedItem.Content is not null)
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

            if (IsSelectedIndexEncoding())
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
                _imageBox.Image = ImageResource.FromImage(_imageFile.GetImage());
                _imageEditorBox.Image = _imageBox.Image;

                _imageBox.Reset();
                _imageEditorBox.Reset();
            }
            catch
            {
                _imageFile = null;
                _imageBox.Image = null;
                _imageEditorBox.Image = null;
            }
        }

        private void UpdateFormInternal()
        {
            _exportBtn.Enabled = _fileStream is not null && _imageFile is not null;

            bool isIndexEncoding = IsSelectedIndexEncoding();

            _paletteOffsetTextBox.Enabled = isIndexEncoding;
            _paletteFormats.Enabled = isIndexEncoding;

            _componentsTextBox.Enabled = _components.ContainsKey(_formats.SelectedItem.Content);
            _paletteComponentsTextBox.Enabled = isIndexEncoding && _paletteComponents.ContainsKey(_paletteFormats.SelectedItem.Content);

            bool hasSwizzleParameter = _swizzleParameterItems.Contains(_swizzles.SelectedItem);
            bool isCustomSizzle = IsCustomSwizzle();

            _renderSwizzleBox.Enabled = _swizzles.SelectedItem.Content is not null || isCustomSizzle;

            _swizzleTextBox.Enabled = hasSwizzleParameter;
            _mainLayout.Items[1] = isCustomSizzle ? _imageEditorBox : _imageBox;
        }

        private void UpdateComponents()
        {
            if (!_components.TryGetValue(_formats.SelectedItem.Content, out string? components))
                return;

            _componentsTextBox.Text = components;
        }

        private void UpdatePaletteComponents()
        {
            if (!_paletteComponents.TryGetValue(_paletteFormats.SelectedItem.Content, out string? components))
                return;

            _paletteComponentsTextBox.Text = components;
        }

        private bool IsSelectedIndexEncoding()
        {
            return _encodingDefinition.ContainsIndexEncoding(_formats.SelectedItem.Content);
        }

        private bool IsCustomSwizzle()
        {
            return _swizzles.SelectedItem == _customSwizzleItem;
        }

        private (int, int)[] GetCustomSwizzleCoordinates()
        {
            return _imageEditorBox.Coordinates.Select(x => ((int)x.X, (int)x.Y)).ToArray();
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

            if (!textBox.Text.StartsWith("0x"))
                return int.TryParse(textBox.Text, out dimension) ? dimension : 0;

            if (textBox.Text.Length <= 2)
                return 0;

            return int.TryParse(textBox.Text[2..], NumberStyles.HexNumber, null, out dimension) ? dimension : 0;
        }

        private byte[] ReadImageData(int offset, Size size, int bitDepth, CreatePixelRemapperDelegate? swizzleDelegate)
        {
            int dataLength = size.Width * size.Height * bitDepth / 8;

            if (_fileStream is null)
                return new byte[dataLength];

            IImageSwizzle? swizzle = CreateSwizzle(swizzleDelegate);
            if (swizzle is not null)
                size = new Size(SizePadding.Multiple(size.Width, swizzle.MacroTileWidth), SizePadding.Multiple(size.Height, swizzle.MacroTileHeight));

            if (offset + dataLength > _fileStream.Length)
                return new byte[dataLength];

            _fileStream.Position = offset;

            var buffer = new byte[dataLength];
            _ = _fileStream.Read(buffer);

            return buffer;
        }

        private byte[] ReadPaletteData(int offset, int colorCount, int bitDepth)
        {
            if (_fileStream is null)
                return [];

            int dataLength = colorCount * bitDepth / 8;
            dataLength = (int)Math.Min(dataLength, _fileStream.Length - offset);

            _fileStream.Position = offset;

            var buffer = new byte[dataLength];
            _ = _fileStream.Read(buffer);

            return buffer;
        }

        private IImageSwizzle? CreateSwizzle(CreatePixelRemapperDelegate? swizzleDelegate)
        {
            IEncodingInfo encoding = _encodingDefinition.ContainsColorEncoding(_formats.SelectedItem.Content)
                ? _encodingDefinition.GetColorEncoding(_formats.SelectedItem.Content)!
                : _encodingDefinition.GetIndexEncoding(_formats.SelectedItem.Content)!.IndexEncoding;
            var options = new SwizzleOptions { EncodingInfo = encoding, Size = GetImageSize() };
            return swizzleDelegate?.Invoke(options);
        }

        private async Task<string?> SelectFile()
        {
            var ofd = new WindowsOpenFileDialog { InitialDirectory = SettingsResources.LastDirectory };

            // Show dialog and wait for result
            var result = await ofd.ShowAsync();
            if (result != DialogResult.Ok)
                return null;

            // Set last visited directory
            SettingsResources.LastDirectory = Path.GetDirectoryName(ofd.Files[0]);

            return ofd.Files[0];
        }
    }

    class CustomSwizzle : IImageSwizzle
    {
        private readonly MasterSwizzle _swizzle;

        /// <inheritdoc />
        public int Width { get; }

        /// <inheritdoc />
        public int Height { get; }

        /// <inheritdoc />
        public int MacroTileWidth => _swizzle.MacroTileWidth;

        /// <inheritdoc />
        public int MacroTileHeight => _swizzle.MacroTileHeight;

        public CustomSwizzle(SwizzleOptions options, MasterSwizzle swizzle)
        {
            Width = options.Size.Width;
            Height = options.Size.Height;

            _swizzle = swizzle;
        }

        /// <inheritdoc />
        public Point Transform(Point point) => Get(point.Y * Width + point.X);

        /// <inheritdoc />
        public Point Get(int pointCount) => _swizzle.Get(pointCount);
    }
}
