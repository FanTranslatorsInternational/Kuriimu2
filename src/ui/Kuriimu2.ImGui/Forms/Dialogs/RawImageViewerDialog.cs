using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using ImGui.Forms.Controls.Text;
using ImGui.Forms.Modals.IO.Windows;
using ImGui.Forms.Modals;
using ImGui.Forms.Resources;
using Kanvas.Contract.Encoding;
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

            _exportBtn.Clicked += _exportBtn_Clicked;

            _widthTextBox.TextChanged += _widthTextBox_TextChanged;
            _heightTextBox.TextChanged += _heightTextBox_TextChanged;
            _offsetTextBox.TextChanged += _offsetTextBox_TextChanged;
            _paletteOffsetTextBox.TextChanged += _paletteOffsetTextBox_TextChanged;
            _formats.SelectedItemChanged += _formats_SelectedItemChanged;
            _paletteFormats.SelectedItemChanged += _paletteFormats_SelectedItemChanged;
            _componentsTextBox.TextChanged += _componentsTextBox_TextChanged;
            _paletteComponentsTextBox.TextChanged += _paletteComponentsTextBox_TextChanged;

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

        private void RawImageViewerDialog_DragDrop(object? sender, Veldrid.Sdl2.DragDropEvent[] e)
        {
            _fileStream?.Dispose();

            _fileStream = File.OpenRead(e[0].File);

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

        private void UpdatePreview()
        {
            if (_fileStream is null)
                return;

            int bitDepth = _encodingDefinition.ContainsColorEncoding(_formats.SelectedItem.Content)
                ? _encodingDefinition.GetColorEncoding(_formats.SelectedItem.Content)!.BitDepth
                : _encodingDefinition.GetIndexEncoding(_formats.SelectedItem.Content)!.IndexEncoding.BitDepth;
            int offset = GetValue(_offsetTextBox);
            Size imageSize = GetImageSize();

            byte[] imageData = ReadImageData(offset, imageSize, bitDepth);

            var imageInfo = new ImageFileInfo
            {
                BitDepth = bitDepth,
                ImageData = imageData,
                ImageFormat = _formats.SelectedItem.Content,
                ImageSize = imageSize
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
            }
            catch
            {
                _imageFile = null;
                _imageBox.Image = null;
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

        private byte[] ReadImageData(int offset, Size size, int bitDepth)
        {
            if (_fileStream is null)
                return [];

            int dataLength = size.Width * size.Height * bitDepth / 8;

            if (offset + dataLength > _fileStream.Length)
                return [];

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

            if (offset + dataLength >= _fileStream.Length)
                return [];

            _fileStream.Position = offset;

            var buffer = new byte[dataLength];
            _ = _fileStream.Read(buffer);

            return buffer;
        }
    }
}
