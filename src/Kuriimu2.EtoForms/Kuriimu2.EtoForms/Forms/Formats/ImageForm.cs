using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using Kontract.Extensions;
using Kontract.Interfaces.Plugins.State;
using Kontract.Kanvas;
using Kontract.Models.Image;
using Kontract.Models.IO;
using Kuriimu2.EtoForms.Extensions;
using Kuriimu2.EtoForms.Forms.Interfaces;
using Kuriimu2.EtoForms.Forms.Models;
using Kuriimu2.EtoForms.Resources;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace Kuriimu2.EtoForms.Forms.Formats
{
    partial class ImageForm : Panel, IKuriimuForm
    {
        private readonly FormInfo _formInfo;
        private readonly AsyncOperation _asyncOperation;

        private int _selectedImageIndex;

        #region Constants

        private const string PngFileFilter_ = "Portable Network Graphics (*.png)|*.png";

        private const string ExportPngTitle_ = "Export Png...";
        private const string ImportPngTitle_ = "Import Png...";

        private const string ExportPaletteTitle_ = "Export palette...";
        private const string ImportPaletteTitle_ = "Import palette...";

        #endregion

        public ImageForm(FormInfo formInfo)
        {
            InitializeComponent();

            _formInfo = formInfo;

            _asyncOperation = new AsyncOperation();
            _asyncOperation.Started += asyncOperation_Started;
            _asyncOperation.Finished += asyncOperation_Finished;

            UpdateImageList();
            UpdateFormats();
            UpdatePalettes(GetSelectedImage());

            UpdateImagePreview(GetSelectedImage());

            UpdateFormInternal();

            images.SelectedIndexChanged += Images_SelectedIndexChanged;
            formats.SelectedValueChanged += Formats_SelectedValueChanged;
            palettes.SelectedValueChanged += Palettes_SelectedValueChanged;

            imagePalette.ChoosingColor += ImagePalette_ChoosingColor;
            imagePalette.PaletteChanged += ImagePalette_PaletteChanged;

            saveCommand.Executed += SaveCommand_Executed;
            saveAsCommand.Executed += SaveAsCommand_Executed;

            exportCommand.Executed += ExportCommand_Executed;
            importCommand.Executed += ImportCommand_Executed;
        }

        #region Forminterface methods

        public bool HasRunningOperations()
        {
            return _asyncOperation.IsRunning;
        }

        #endregion

        #region Update

        private void UpdateImageList()
        {
            images.DataStore = GetStateImages().Select((x, i) =>
                new ImageElement(GenerateThumbnail(x.GetImage().ToEto()), x.Name ?? $"{i:00}")).ToArray();
        }

        private void UpdateFormats()
        {
            var definition = GetEncodingDefinition();

            IEnumerable<ImageEncodingElement> elements = Array.Empty<ImageEncodingElement>();
            if (definition.HasColorEncodings)
                elements = elements.Concat(
                    definition.ColorEncodings.Select(x => new ImageEncodingElement(x.Key, x.Value)));
            if (definition.HasIndexEncodings)
                elements = elements.Concat(
                    definition.IndexEncodings.Select(x => new ImageEncodingElement(x.Key, x.Value)));

            formats.DataStore = elements;
        }

        private void UpdatePalettes(IKanvasImage image)
        {
            if (image == null)
            {
                palettes.DataStore = Array.Empty<ImageEncodingElement>();
                return;
            }

            var definition = GetEncodingDefinition();

            IEnumerable<ImageEncodingElement> elements = Array.Empty<ImageEncodingElement>();
            if (definition.HasPaletteEncodings)
            {
                var paletteEncodings = definition.PaletteEncodings;
                var encodingIndices = definition.GetIndexEncoding(image.ImageFormat).PaletteEncodingIndices;

                if (image.IsIndexed && encodingIndices.Any())
                    paletteEncodings = paletteEncodings.Where(x => encodingIndices.Contains(x.Key))
                        .ToDictionary(x => x.Key, y => y.Value);

                elements = elements.Concat(paletteEncodings.Select(x => new ImageEncodingElement(x.Key, x.Value)));
            }

            palettes.DataStore = elements;
        }

        private void UpdateImagePreview(IKanvasImage image)
        {
            if (image == null)
                return;

            var definition = GetEncodingDefinition();

            // Set dropdown values
            formats.Text = definition.GetColorEncoding(image.ImageFormat)?.FormatName ??
                           definition.GetIndexEncoding(image.ImageFormat).IndexEncoding.FormatName;

            if (image.IsIndexed)
                palettes.Text = definition.GetPaletteEncoding(image.PaletteFormat).FormatName;

            // Set size
            width.Text = image.ImageSize.Width.ToString();
            height.Text = image.ImageSize.Height.ToString();

            // Set image
            imageView.Image = image.GetImage().ToEto();
            imageView.Invalidate();

            // Update palette image
            if (image.IsIndexed)
                UpdatePaletteImage();
        }

        private void ToggleForm(bool toggle)
        {
            formats.Enabled = toggle;
            palettes.Enabled = toggle;
            images.Enabled = toggle;
            imageView.Enabled = toggle;

            saveButton.Enabled = toggle;
            saveAsButton.Enabled = toggle;
            exportButton.Enabled = toggle;
            importButton.Enabled = toggle;

            imagePalette.Enabled = toggle;
        }

        public void UpdateForm()
        {
            UpdateFormInternal();
        }

        private void UpdateFormInternal()
        {
            var selectedImage = GetSelectedImage();

            saveButton.Enabled = selectedImage != null && _formInfo.FileState.PluginState.CanSave;
            saveAsButton.Enabled = selectedImage != null && _formInfo.FileState.PluginState.CanSave && _formInfo.FileState.ParentFileState == null;

            exportButton.Enabled = selectedImage != null;
            importButton.Enabled = selectedImage != null && _formInfo.FileState.PluginState.CanSave;

            var definition = GetEncodingDefinition();
            var isIndexed = selectedImage?.IsIndexed ?? false;
            var isLocked = selectedImage?.IsImageLocked ?? false;
            palettes.Enabled = !isLocked && isIndexed && definition.HasPaletteEncodings;
            formats.Enabled = !isLocked && definition.HasColorEncodings || definition.HasIndexEncodings;

            imageView.Enabled = GetStateImages().Any();
            images.Enabled = GetStateImages().Any();

            imagePalette.Enabled = isIndexed;
        }

        private void UpdatePaletteImage()
        {
            var selectedImage = GetSelectedImage();
            if (!selectedImage.IsIndexed)
            {
                imagePalette.Palette = null;
                return;
            }

            imagePalette.Palette = selectedImage.GetPalette(_formInfo.Progress);
        }

        #endregion

        #region Save

        private async Task SaveAs()
        {
            await Save(true);
        }

        private async Task Save(bool saveAs = false)
        {
            var wasSuccessful = await _formInfo.FormCommunicator.Save(saveAs);
            if (!wasSuccessful)
                return;

            UpdateFormInternal();
            _formInfo.FormCommunicator.Update(true, false);
        }

        #endregion

        #region Export

        private void ExportPng()
        {
            var selectedImage = GetSelectedImage();
            var imageName = string.IsNullOrEmpty(selectedImage.Name) ?
                _formInfo.FileState.FilePath.GetNameWithoutExtension() + "." + _selectedImageIndex.ToString("00") + ".png" :
                selectedImage.Name;

            var sfd = new SaveFileDialog
            {
                Title = ExportPngTitle_,
                Directory = Settings.Default.LastDirectory == string.Empty ? new Uri(Path.GetFullPath(".")) : new Uri(Settings.Default.LastDirectory),
                FileName = imageName,
                Filters = { new FileFilter("Portable Network Graphic (*.png)", "*.png") }
            };

            if (sfd.ShowDialog(this) != DialogResult.Ok)
                return;

            selectedImage.GetImage(_formInfo.Progress).Save(sfd.FileName, ImageFormat.Png);
        }

        #endregion

        #region Import

        private async Task ImportPng()
        {
            var ofd = new OpenFileDialog
            {
                Title = ImportPngTitle_,
                Directory = Settings.Default.LastDirectory == string.Empty ? new Uri(Path.GetFullPath(".")) : new Uri(Settings.Default.LastDirectory),
                Filters = { new FileFilter("Portable Network Graphic (*.png)", "*.png") }
            };

            if (ofd.ShowDialog(this) != DialogResult.Ok)
                return;

            await Import(ofd.FileName);
        }

        private async Task Import(UPath filePath)
        {
            ToggleForm(false);

            try
            {
                using var newImage = new System.Drawing.Bitmap(filePath.FullName);
                await _asyncOperation.StartAsync(cts => GetSelectedImage().SetImage(newImage, _formInfo.Progress));
            }
            catch (Exception ex)
            {
                _formInfo.FormCommunicator.ReportStatus(false, ex.Message);
            }

            UpdateImagePreview(GetSelectedImage());
            UpdateImageList();

            UpdateFormInternal();

            _formInfo.FormCommunicator.Update(true, false);
            _formInfo.FormCommunicator.ReportStatus(true, "Image successfully imported.");
        }

        #endregion

        #region Events

        private async void Formats_SelectedValueChanged(object sender, EventArgs e)
        {
            ToggleForm(false);

            var selectedImage = GetSelectedImage();
            var selectedFormat = GetSelectedImageFormat();
            await _asyncOperation.StartAsync(cts => selectedImage.TranscodeImage(selectedFormat, _formInfo.Progress));

            UpdateImagePreview(GetSelectedImage());
            UpdateFormInternal();

            _formInfo.FormCommunicator.Update(true, false);
        }

        private async void Palettes_SelectedValueChanged(object sender, EventArgs e)
        {
            ToggleForm(false);

            var selectedImage = GetSelectedImage();
            var selectedFormat = GetSelectedPaletteFormat();
            await _asyncOperation.StartAsync(cts => selectedImage.TranscodePalette(selectedFormat, _formInfo.Progress));

            UpdateImagePreview(GetSelectedImage());
            UpdateFormInternal();

            _formInfo.FormCommunicator.Update(true, false);
        }

        private void Images_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedImageIndex = GetSelectedImageIndex();

            UpdateImagePreview(GetSelectedImage());
            UpdatePalettes(GetSelectedImage());
            UpdateFormInternal();
        }

        private void ImagePalette_ChoosingColor(object sender, Controls.ChoosingColorEventArgs e)
        {
            var clrDialog = new ColorDialog();
            if (clrDialog.ShowDialog(this) != DialogResult.Ok)
            {
                e.Cancel = true;
                return;
            }

            var c = clrDialog.Color;
            e.Result = System.Drawing.Color.FromArgb(c.Ab, c.Rb, c.Gb, c.Bb);
        }

        private async void ImagePalette_PaletteChanged(object sender, Controls.PaletteChangedEventArgs e)
        {
            await SetColorInPalette(e.Index, e.NewColor);
        }

        private async void SaveAsCommand_Executed(object sender, EventArgs e)
        {
            await SaveAs();
        }

        private async void SaveCommand_Executed(object sender, EventArgs e)
        {
            await Save();
        }

        private void ExportCommand_Executed(object sender, EventArgs e)
        {
            ExportPng();
        }

        private async void ImportCommand_Executed(object sender, EventArgs e)
        {
            await ImportPng();
        }

        private void asyncOperation_Started(object sender, EventArgs e)
        {
        }

        private void asyncOperation_Finished(object sender, EventArgs e)
        {
        }

        #endregion

        #region Support

        private IList<IKanvasImage> GetStateImages()
        {
            return (_formInfo.FileState.PluginState as IImageState).Images;
        }

        private IKanvasImage GetSelectedImage()
        {
            if (_selectedImageIndex >= GetStateImages().Count || _selectedImageIndex < 0)
                return null;

            return GetStateImages()[_selectedImageIndex];
        }

        private EncodingDefinition GetEncodingDefinition()
        {
            return (_formInfo.FileState.PluginState as IImageState).EncodingDefinition;
        }

        private int GetSelectedImageFormat()
        {
            return (formats.SelectedValue as ImageEncodingElement).ImageIdent;
        }

        private int GetSelectedPaletteFormat()
        {
            return (palettes.SelectedValue as ImageEncodingElement).ImageIdent;
        }

        private int GetSelectedImageIndex()
        {
            return images.SelectedIndex;
        }

        private Bitmap GenerateThumbnail(Image input)
        {
            var thumbWidth = Settings.Default.ThumbnailWidth;
            var thumbHeight = Settings.Default.ThumbnailHeight;
            var thumb = new Bitmap(thumbWidth, thumbHeight, PixelFormat.Format32bppRgba);
            using var gfx = new Graphics(thumb)
            {
                ImageInterpolation = ImageInterpolation.Default
            };


            var wRatio = (float)input.Width / thumbWidth;
            var hRatio = (float)input.Height / thumbHeight;
            var ratio = wRatio >= hRatio ? wRatio : hRatio;

            if (input.Width <= thumbWidth && input.Height <= thumbHeight)
                ratio = 1.0f;

            var size = new Size((int)Math.Min(input.Width / ratio, thumbWidth), (int)Math.Min(input.Height / ratio, thumbHeight));
            var pos = new Point(thumbWidth / 2 - size.Width / 2, thumbHeight / 2 - size.Height / 2);

            gfx.DrawImage(input, pos.X, pos.Y, size.Width, size.Height);

            return thumb;
        }

        #endregion

        #region KanvasImage

        private async Task SetColorInPalette(int index, System.Drawing.Color newColor)
        {
            if(_asyncOperation.IsRunning)
                return;

            var selectedImage = GetSelectedImage();
            if (!selectedImage.IsIndexed)
                return;

            var palette = selectedImage.GetPalette(_formInfo.Progress);
            if (index < 0 || index >= palette.Count)
                return;

            ToggleForm(false);

            try
            {
                await _asyncOperation.StartAsync(cts =>
                {
                    selectedImage.SetColorInPalette(index, newColor);
                    _formInfo.Progress.ReportProgress("Done", 1, 1);
                });
            }
            catch (Exception ex)
            {
                _formInfo.FormCommunicator.ReportStatus(false, ex.Message);
                UpdateFormInternal();

                return;
            }

            UpdateFormInternal();

            UpdateImagePreview(selectedImage);
            UpdateImageList();
        }

        #endregion
    }
}
