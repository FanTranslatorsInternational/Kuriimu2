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
        private readonly FormInfo<IImageState> _formInfo;
        private readonly AsyncOperation _asyncOperation;

        private int _selectedImageIndex;

        private IList<ImageElement> _currentImages;
        private IList<ImageEncodingElement> _currentFormats;
        private IList<ImageEncodingElement> _currentPaletteFormats;

        #region Localization Keys

        private const string ExportPngTitleKey_ = "ExportPngTitle";
        private const string ImportPngTitleKey_ = "ImportPngTitle";

        private const string ImageSuccessfullyImportedStatusKey_ = "ImageSuccessfullyImportedStatus";

        private const string PngFileFilterKey_ = "PngFileFilter";

        #endregion

        public ImageForm(FormInfo<IImageState> formInfo)
        {
            InitializeComponent();

            _formInfo = formInfo;

            _asyncOperation = new AsyncOperation();
            _asyncOperation.Started += asyncOperation_Started;
            _asyncOperation.Finished += asyncOperation_Finished;

            if (GetStateImages().Count > 0)
            {
                LoadFormats(GetSelectedImage());
                LoadPaletteFormats(GetSelectedImage());
                LoadImageList();

                UpdateFormats();
                UpdatePalettes();
                UpdateImageList();

                UpdateImagePreview(GetSelectedImage());
            }

            UpdateFormInternal();

            #region Set Events

            imagePalette.ChoosingColor += ImagePalette_ChoosingColor;
            imagePalette.PaletteChanged += ImagePalette_PaletteChanged;

            saveCommand.Executed += SaveCommand_Executed;
            saveAsCommand.Executed += SaveAsCommand_Executed;

            exportCommand.Executed += ExportCommand_Executed;
            importCommand.Executed += ImportCommand_Executed;

            #endregion
        }

        #region Forminterface methods

        public void UpdateForm()
        {
            UpdateFormInternal();
        }

        public bool HasRunningOperations()
        {
            return _asyncOperation.IsRunning;
        }

        public void CancelOperations()
        {
            if (HasRunningOperations())
                _asyncOperation.Cancel();
        }

        #endregion

        #region Load methods

        private void LoadImageList()
        {
            _currentImages = GetStateImages().Select((x, i) =>
                new ImageElement(GenerateThumbnail(x.GetImage().ToEto()), x.Name ?? $"{i:00}")).ToArray();
        }

        private void LoadFormats(IKanvasImage selectedImage)
        {
            var definition = selectedImage.EncodingDefinition;

            IEnumerable<ImageEncodingElement> elements = Array.Empty<ImageEncodingElement>();
            if (definition.HasColorEncodings)
                elements = elements.Concat(
                    definition.ColorEncodings.Select(x => new ImageEncodingElement(x.Key, x.Value)));
            if (definition.HasIndexEncodings)
                elements = elements.Concat(
                    definition.IndexEncodings.Select(x => new ImageEncodingElement(x.Key, x.Value)));

            _currentFormats = elements.ToArray();
        }

        private void LoadPaletteFormats(IKanvasImage image)
        {
            if (image == null)
            {
                palettes.DataStore = Array.Empty<ImageEncodingElement>();
                return;
            }

            var definition = image.EncodingDefinition;

            IEnumerable<ImageEncodingElement> elements = Array.Empty<ImageEncodingElement>();
            if (image.IsIndexed && definition.HasPaletteEncodings)
            {
                var paletteEncodings = definition.PaletteEncodings;
                var encodingIndices = definition.GetIndexEncoding(image.ImageFormat).PaletteEncodingIndices;

                if (image.IsIndexed && encodingIndices.Any())
                    paletteEncodings = paletteEncodings.Where(x => encodingIndices.Contains(x.Key))
                        .ToDictionary(x => x.Key, y => y.Value);

                elements = elements.Concat(paletteEncodings.Select(x => new ImageEncodingElement(x.Key, x.Value)));
            }

            _currentPaletteFormats = elements.ToArray();
        }

        #endregion

        #region Update

        private void UpdateFormats()
        {
            formats.SelectedValueChanged -= Formats_SelectedValueChanged;

            formats.DataStore = _currentFormats;

            formats.SelectedValueChanged += Formats_SelectedValueChanged;
        }

        private void UpdatePalettes()
        {
            palettes.SelectedValueChanged -= Palettes_SelectedValueChanged;

            palettes.DataStore = _currentPaletteFormats;

            palettes.SelectedValueChanged += Palettes_SelectedValueChanged;
        }

        private void UpdateImageList()
        {
            imageList.SelectedIndexChanged -= ImageList_SelectedIndexChanged;

            imageList.DataStore = _currentImages;

            imageList.SelectedIndexChanged += ImageList_SelectedIndexChanged;
        }

        private void UpdateSelectedImagePreview()
        {
            UpdateImagePreview(GetSelectedImage());
        }

        private void UpdateImagePreview(IKanvasImage image)
        {
            if (image == null)
                return;

            // Set dropdown values
            UpdateSelectedImageFormats();

            // Set size
            width.Text = image.ImageSize.Width.ToString();
            height.Text = image.ImageSize.Height.ToString();

            // Set image
            imageView.Image = image.GetImage().ToEto();
            imageView.Invalidate();
        }

        private void UpdateSelectedPaletteImage()
        {
            UpdatePaletteImage(GetSelectedImage());
        }

        private void UpdatePaletteImage(IKanvasImage image)
        {
            if (!image.IsIndexed)
            {
                imagePalette.Palette = null;
                return;
            }

            imagePalette.Palette = image.GetPalette(_formInfo.Progress);
        }

        private void UpdateSelectedThumbnail()
        {
            _currentImages[_selectedImageIndex].UpdateThumbnail(GenerateThumbnail(GetSelectedImage().GetImage().ToEto()));
        }

        private void UpdateSelectedImageFormats()
        {
            formats.SelectedValueChanged -= Formats_SelectedValueChanged;
            palettes.SelectedValueChanged -= Palettes_SelectedValueChanged;

            formats.SelectedValue = _currentFormats.FirstOrDefault(x => x.ImageIdent == GetSelectedImage().ImageFormat);
            palettes.SelectedValue = _currentPaletteFormats.FirstOrDefault(x => x.ImageIdent == GetSelectedImage().PaletteFormat);

            formats.SelectedValueChanged += Formats_SelectedValueChanged;
            palettes.SelectedValueChanged += Palettes_SelectedValueChanged;
        }

        private void ToggleForm(bool toggle)
        {
            // Toggle button operation availability
            saveButton.Enabled = toggle;
            saveAsButton.Enabled = toggle;
            exportButton.Enabled = toggle;
            importButton.Enabled = toggle;

            // Toggle format dropdown availability
            formats.Enabled = toggle;
            palettes.Enabled = toggle;

            // Toggle image related availability
            imageList.Enabled = toggle;
            imageView.Enabled = toggle;
            imagePalette.Enabled = toggle;
        }

        private void UpdateFormInternal()
        {
            var selectedImage = GetSelectedImage();

            // Update button operation availability
            saveButton.Enabled = selectedImage != null && _formInfo.FileState.PluginState.CanSave;
            saveAsButton.Enabled = selectedImage != null && _formInfo.FileState.PluginState.CanSave && _formInfo.FileState.ParentFileState == null;

            exportButton.Enabled = selectedImage != null;
            importButton.Enabled = selectedImage != null && _formInfo.FileState.PluginState.CanSave;

            // Update format dropdown availability
            var definition = selectedImage?.EncodingDefinition;
            var isIndexed = selectedImage?.IsIndexed ?? false;
            var isLocked = selectedImage?.IsImageLocked ?? false;
            palettes.Enabled = !isLocked && isIndexed && definition.HasPaletteEncodings;
            formats.Enabled = !isLocked && (definition?.HasColorEncodings ?? false) || (definition?.HasIndexEncodings ?? false);

            // Update image related availability
            imageView.Enabled = GetStateImages().Any();
            imageList.Enabled = GetStateImages().Any();
            imagePalette.Enabled = isIndexed;
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
                selectedImage.Name + ".png";

            var sfd = new SaveFileDialog
            {
                Title = Localize(ExportPngTitleKey_),
                Directory = Settings.Default.LastDirectory == string.Empty ? new Uri(Path.GetFullPath(".")) : new Uri(Settings.Default.LastDirectory),
                FileName = imageName,
                Filters = { new FileFilter(Localize(PngFileFilterKey_), "*.png") }
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
                Title = Localize(ImportPngTitleKey_),
                Directory = Settings.Default.LastDirectory == string.Empty ? new Uri(Path.GetFullPath(".")) : new Uri(Settings.Default.LastDirectory),
                Filters = { new FileFilter(Localize(PngFileFilterKey_), "*.png") }
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
                var newImage = new System.Drawing.Bitmap(filePath.FullName);
                await _asyncOperation.StartAsync(cts => GetSelectedImage().SetImage(newImage, _formInfo.Progress));
            }
            catch (Exception ex)
            {
                _formInfo.FormCommunicator.ReportStatus(false, ex.Message);
            }

            UpdateSelectedThumbnail();
            UpdateSelectedImagePreview();

            UpdateFormInternal();

            _formInfo.FormCommunicator.Update(true, false);
            _formInfo.FormCommunicator.ReportStatus(true, Localize(ImageSuccessfullyImportedStatusKey_));
        }

        #endregion

        #region Events

        private async void Formats_SelectedValueChanged(object sender, EventArgs e)
        {
            ToggleForm(false);

            var selectedImage = GetSelectedImage();
            var selectedFormat = GetSelectedImageFormat();
            await _asyncOperation.StartAsync(cts => selectedImage.TranscodeImage(selectedFormat, _formInfo.Progress));

            LoadPaletteFormats(GetSelectedImage());
            UpdatePalettes();

            UpdateSelectedThumbnail();
            UpdateSelectedImagePreview();
            UpdateSelectedPaletteImage();

            UpdateFormInternal();

            _formInfo.FormCommunicator.Update(true, false);
        }

        private async void Palettes_SelectedValueChanged(object sender, EventArgs e)
        {
            ToggleForm(false);

            var selectedImage = GetSelectedImage();
            var selectedFormat = GetSelectedPaletteFormat();
            await _asyncOperation.StartAsync(cts => selectedImage.TranscodePalette(selectedFormat, _formInfo.Progress));

            UpdateSelectedThumbnail();
            UpdateSelectedImagePreview();
            UpdateSelectedPaletteImage();

            UpdateFormInternal();

            _formInfo.FormCommunicator.Update(true, false);
        }

        private void ImageList_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedImageIndex = GetSelectedImageIndex();

            // Change format information to newly selected image
            LoadFormats(GetSelectedImage());
            UpdateFormats();

            LoadPaletteFormats(GetSelectedImage());
            UpdatePalettes();

            UpdateSelectedImageFormats();

            // Update remaining form
            UpdateSelectedImagePreview();

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

        private string Localize(string name, params object[] args)
        {
            return string.Format(Application.Instance.Localize(this, name), args);
        }

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
            return imageList.SelectedIndex;
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
            if (_asyncOperation.IsRunning)
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
                    _formInfo.Progress.ReportProgress(1, 1);
                });
            }
            catch (Exception ex)
            {
                _formInfo.FormCommunicator.ReportStatus(false, ex.Message);
                UpdateFormInternal();

                return;
            }

            UpdateSelectedThumbnail();
            UpdateSelectedImagePreview();

            UpdateFormInternal();
        }

        #endregion
    }
}
