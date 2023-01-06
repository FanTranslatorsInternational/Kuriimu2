using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using ImGui.Forms;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Lists;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using Kontract.Interfaces.Progress;
using Kontract.Kanvas.Interfaces;
using Kuriimu2.ImGui.Resources;
using ImageResources = Kuriimu2.ImGui.Resources.ImageResources;
using Size = ImGui.Forms.Models.Size;

namespace Kuriimu2.ImGui.Forms.Formats
{
    partial class ImageForm
    {
        private StackLayout _mainLayout;

        private ImageButton _saveBtn;
        private ImageButton _saveAsBtn;

        private Label _widthTextLbl;
        private Label _heightTextLbl;
        private Label _widthContentLbl;
        private Label _heightContentLbl;

        private Label _formatTextLbl;
        private Label _paletteTextLbl;
        private ComboBox<int> _formatBox;
        private ComboBox<int> _paletteBox;

        private ZoomablePictureBox _imageBox;

        private ImageList _imgList;

        private void InitializeComponent()
        {
            #region Controls

            _widthTextLbl = new Label { Text = LocalizationResources.ImageLabelWidth() };
            _heightTextLbl = new Label { Text = LocalizationResources.ImageLabelHeight() };
            _widthContentLbl = new Label();
            _heightContentLbl = new Label();

            _formatTextLbl = new Label { Text = LocalizationResources.ImageLabelFormat() };
            _paletteTextLbl = new Label { Text = LocalizationResources.ImageLabelPalette() };
            _formatBox = new ComboBox<int>();
            _paletteBox = new ComboBox<int>();

            _imageBox = new ZoomablePictureBox { ShowBorder = true };

            _imgList = new ImageList { ItemPadding = 4, ThumbnailSize = new Vector2(90, 60), ShowThumbnailBorder = true };

            _saveBtn = new ImageButton { Image = ImageResources.Save(Style.Theme), ImageSize = new Vector2(16, 16), Padding = new Vector2(5, 5), Enabled = false };
            _saveAsBtn = new ImageButton { Image = ImageResources.SaveAs(Style.Theme), ImageSize = new Vector2(16, 16), Padding = new Vector2(5, 5), Enabled = false };

            #endregion

            _mainLayout = new StackLayout
            {
                Alignment = Alignment.Horizontal,
                ItemSpacing = 4,
                Items =
                {
                    new StackLayout
                    {
                        Alignment = Alignment.Vertical,
                        ItemSpacing = 4,
                        Size = new Size(.70f,SizeValue.Parent),
                        Items =
                        {
                            new StackLayout
                            {
                                Alignment = Alignment.Horizontal,
                                ItemSpacing = 4,
                                Size = Size.WidthAlign,
                                Items =
                                {
                                    _saveBtn,
                                    _saveAsBtn
                                }
                            },
                            _imageBox,
                            new TableLayout
                            {
                                Spacing = new Vector2(4,4),
                                Size = Size.WidthAlign,
                                Rows =
                                {
                                    new TableRow
                                    {
                                        Cells =
                                        {
                                            _widthTextLbl,
                                            _widthContentLbl,
                                            _heightTextLbl,
                                            _heightContentLbl
                                        }
                                    },
                                    new TableRow
                                    {
                                        Cells =
                                        {
                                            _formatTextLbl,
                                            _formatBox,
                                            _paletteTextLbl,
                                            _paletteBox
                                        }
                                    }
                                }
                            }
                        }
                    },
                    new StackLayout
                    {
                        Alignment = Alignment.Vertical,
                        ItemSpacing = 4,
                        Size = new Size(.3f,SizeValue.Parent),
                        Items =
                        {
                            _imgList
                        }
                    }
                }
            };
        }

        private void SetImages(IReadOnlyList<IImageInfo> images, IProgressContext progress)
        {
            _imgList.Items.Clear();
            _imgList.SelectedItem = null;

            if (images == null || images.Count <= 0)
                return;

            var perPart = 100f / images.Count;
            var perStart = 0f;

            foreach (var img in images)
            {
                var scopeProgress = progress.CreateScope(LocalizationResources.ImageProgressDecode(), perStart, perStart + perPart);

                _imgList.Items.Add(new FormImageListItem(img, img.GetImage(scopeProgress)));

                perStart += perPart;
            }
        }

        private void SetSelectedImage(IImageInfo img, IProgressContext progress)
        {
            SetFormats(img);
            SetPaletteFormats(img);

            _imgList.SelectedItem = _imgList.Items.FirstOrDefault(x => ((FormImageListItem)x).ImageInfo == img);
            SetImage(img, progress);

            _widthContentLbl.Text = img.ImageSize.Width.ToString();
            _heightContentLbl.Text = img.ImageSize.Height.ToString();
        }

        private void SetImage(IImageInfo img, IProgressContext progress)
        {
            _imageBox.Image = ImageResource.FromBitmap(img.GetImage(progress));
            _imgList.SelectedItem.Image = ImageResource.FromBitmap(img.GetImage(progress));
        }

        private void SetFormats(IImageInfo img)
        {
            _formatBox.Items.Clear();
            _formatBox.SelectedItem = null;

            if (img == null)
                return;

            var hasFormats = img.EncodingDefinition.HasColorEncodings || img.EncodingDefinition.HasIndexEncodings;
            _formatBox.Visible = _formatTextLbl.Visible = hasFormats;

            if (!hasFormats)
                return;

            if (img.EncodingDefinition.HasColorEncodings)
                foreach (var colorEnc in img.EncodingDefinition.ColorEncodings)
                    _formatBox.Items.Add(new ComboBoxItem<int>(colorEnc.Key, colorEnc.Value.FormatName));

            if (img.EncodingDefinition.HasIndexEncodings)
                foreach (var indexEnc in img.EncodingDefinition.IndexEncodings)
                    _formatBox.Items.Add(new ComboBoxItem<int>(indexEnc.Key, indexEnc.Value.IndexEncoding.FormatName));

            _formatBox.SelectedItem = _formatBox.Items.FirstOrDefault(x => x.Content == img.ImageFormat);
        }

        private void SetPaletteFormats(IImageInfo img)
        {
            _paletteBox.Items.Clear();
            _paletteBox.SelectedItem = null;

            if (img == null)
                return;

            var hasPalettes = img.EncodingDefinition.HasPaletteEncodings;
            _paletteBox.Visible = _paletteTextLbl.Visible = hasPalettes;

            if (!hasPalettes)
                return;

            if (img.EncodingDefinition.HasPaletteEncodings)
                foreach (var paletteEnc in img.EncodingDefinition.PaletteEncodings)
                    _paletteBox.Items.Add(new ComboBoxItem<int>(paletteEnc.Key, paletteEnc.Value.FormatName));

            _paletteBox.SelectedItem = _paletteBox.Items.FirstOrDefault(x => x.Content == img.PaletteFormat);
        }

        class FormImageListItem : ImageListItem
        {
            public IImageInfo ImageInfo { get; }

            public FormImageListItem(IImageInfo img, Bitmap cachedImage = null)
            {
                ImageInfo = img;

                Image = ImageResource.FromBitmap(cachedImage ?? img.GetImage());
                Text = img.Name;
            }
        }
    }
}
