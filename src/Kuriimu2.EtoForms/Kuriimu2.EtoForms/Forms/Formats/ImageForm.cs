using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using Kanvas;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Progress;
using Kontract.Kanvas;
using Kontract.Models.Image;
using Kuriimu2.EtoForms.Extensions;
using Kuriimu2.EtoForms.Forms.Interfaces;
using Kuriimu2.EtoForms.Forms.Models;
using Kuriimu2.EtoForms.Resources;

namespace Kuriimu2.EtoForms.Forms.Formats
{
    partial class ImageForm : Panel, IKuriimuForm
    {
        private readonly IStateInfo _stateInfo;
        private readonly IFormCommunicator _communicator;
        private readonly IProgressContext _progress;

        private int _selectedImageIndex;

        #region Constants

        private const string MenuSaveResourceName = "Kuriimu2.EtoForms.Images.menu-save.png";
        private const string MenuSaveAsResourceName = "Kuriimu2.EtoForms.Images.menu-save-as.png";
        private const string MenuExportResourceName = "Kuriimu2.EtoForms.Images.image-export.png";
        private const string MenuImportResourceName = "Kuriimu2.EtoForms.Images.image-import.png";

        #endregion

        #region Loaded image resources

        private readonly Image MenuSaveResource = Bitmap.FromResource(MenuSaveResourceName);
        private readonly Image MenuSaveAsResource = Bitmap.FromResource(MenuSaveAsResourceName);
        private readonly Image MenuExportResource = Bitmap.FromResource(MenuExportResourceName);
        private readonly Image MenuImportResource = Bitmap.FromResource(MenuImportResourceName);

        #endregion

        public ImageForm(IStateInfo stateInfo, IFormCommunicator communicator, IProgressContext progress)
        {
            InitializeComponent();

            _stateInfo = stateInfo;
            _communicator = communicator;
            _progress = progress;

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
        }

        #region Update

        private void UpdateImageList()
        {
            images.DataStore = GetStateImages().Select((x, i) => new ImageElement(GenerateThumbnail(x.GetImage().ToEto()), x.Name ?? $"{i:00}")).ToArray();
        }

        private void UpdateFormats()
        {
            var definition = GetEncodingDefinition();

            IEnumerable<ImageEncodingElement> elements = Array.Empty<ImageEncodingElement>();
            if (definition.HasColorEncodings)
                elements = elements.Concat(definition.ColorEncodings.Select(x => new ImageEncodingElement(x.Key, x.Value)));
            if (definition.HasIndexEncodings)
                elements = elements.Concat(definition.IndexEncodings.Select(x => new ImageEncodingElement(x.Key, x.Value)));

            formats.DataStore = elements;
        }

        private void UpdatePalettes(IKanvasImage image)
        {
            var definition = GetEncodingDefinition();

            IEnumerable<ImageEncodingElement> elements = Array.Empty<ImageEncodingElement>();
            if (definition.HasPaletteEncodings)
            {
                var paletteEncodings = definition.PaletteEncodings;
                var encodingIndices = definition.GetIndexEncoding(image.ImageFormat).PaletteEncodingIndices;

                if (image.IsIndexed && encodingIndices.Any())
                    paletteEncodings = paletteEncodings.Where(x => encodingIndices.Contains(x.Key)).ToDictionary(x => x.Key, y => y.Value);

                elements = elements.Concat(paletteEncodings.Select(x => new ImageEncodingElement(x.Key, x.Value)));
            }

            palettes.DataStore = elements;
        }

        private void UpdateImagePreview(IKanvasImage image)
        {
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
            saveButton.Enabled = _stateInfo.PluginState is ISaveFiles;
            saveAsButton.Enabled = _stateInfo.PluginState is ISaveFiles && _stateInfo.ParentStateInfo == null;

            exportButton.Enabled = true;
            importButton.Enabled = true;

            var definition = GetEncodingDefinition();
            var isIndexed = GetSelectedImage().IsIndexed;
            palettes.Enabled = isIndexed && definition.HasPaletteEncodings;
            formats.Enabled = definition.HasColorEncodings || definition.HasIndexEncodings;

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

            imagePalette.Palette = selectedImage.GetPalette(_progress);
        }

        #endregion

        #region Events

        private async void Formats_SelectedValueChanged(object sender, EventArgs e)
        {
            ToggleForm(false);

            var selectedImage = GetSelectedImage();
            var selectedFormat = GetSelectedImageFormat();
            await Task.Run(() => selectedImage.TranscodeImage(selectedFormat, _progress));

            UpdateImagePreview(GetSelectedImage());
            UpdateFormInternal();

            _communicator.Update(true, false);
        }

        private async void Palettes_SelectedValueChanged(object sender, EventArgs e)
        {
            ToggleForm(false);

            var selectedImage = GetSelectedImage();
            var selectedFormat = GetSelectedPaletteFormat();
            await Task.Run(() => selectedImage.TranscodePalette(selectedFormat, _progress));

            UpdateImagePreview(GetSelectedImage());
            UpdateFormInternal();

            _communicator.Update(true, false);
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

        private void ImagePalette_PaletteChanged(object sender, Controls.PaletteChangedEventArgs e)
        {
            SetColorInPalette(e.Index, e.NewColor);
        }

        #endregion

        #region Support

        private IList<IKanvasImage> GetStateImages()
        {
            return (_stateInfo.PluginState as IImageState).Images;
        }

        private IKanvasImage GetSelectedImage()
        {
            return GetStateImages()[_selectedImageIndex];
        }

        private EncodingDefinition GetEncodingDefinition()
        {
            return (_stateInfo.PluginState as IImageState).EncodingDefinition;
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

        private void SetColorInPalette(int index, System.Drawing.Color newColor)
        {
            var selectedImage = GetSelectedImage();
            if (!selectedImage.IsIndexed)
                return;

            var palette = selectedImage.GetPalette(_progress);
            if (index < 0 || index >= palette.Count)
                return;

            ToggleForm(false);

            try
            {
                Task.Run(() =>
                {
                    selectedImage.SetColorInPalette(index, newColor);
                    _progress.ReportProgress("Done", 1, 1);
                }).Wait();
            }
            catch (Exception ex)
            {
                _communicator.ReportStatus(false, ex.Message);
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
