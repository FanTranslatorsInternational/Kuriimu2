using Hexa.NET.ImGui;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Models;
using ImGui.Forms.Models.IO;
using ImGui.Forms.Resources;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Contract.Progress;
using Kuriimu2.ImGui.Components;
using Kuriimu2.ImGui.Resources;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using ImageResources = Kuriimu2.ImGui.Resources.ImageResources;
using Size = ImGui.Forms.Models.Size;

namespace Kuriimu2.ImGui.Forms.Formats
{
    internal partial class ImageForm
    {
        private StackLayout _mainLayout;
        private StackLayout _listPaletteLayout;
        private StackLayout _imageInfoLayout;

        private ImageButton _saveBtn;
        private ImageButton _saveAsBtn;
        private ImageButton _imgExportBtn;
        private ImageButton _imgImportBtn;
        private ImageButton _batchImgExportBtn;
        private ImageButton _batchImgImportBtn;

        private Label _widthTextLbl;
        private Label _heightTextLbl;
        private Label _widthContentLbl;
        private Label _heightContentLbl;

        private Label _formatTextLbl;
        private Label _paletteTextLbl;
        private ComboBox<int> _formatBox;
        private ComboBox<int> _paletteBox;

        private ZoomablePictureBox _imageBox;
        private ZoomableIndexedPictureBox _indexedImageBox;
        private PaletteView _paletteView;

        private global::ImGui.Forms.Controls.Lists.List<ImageThumbnail> _imgList;

        [MemberNotNull(nameof(_mainLayout), nameof(_listPaletteLayout), nameof(_imageInfoLayout))]
        [MemberNotNull(nameof(_saveBtn), nameof(_saveAsBtn), nameof(_imgExportBtn), nameof(_imgImportBtn), nameof(_batchImgExportBtn), nameof(_batchImgImportBtn))]
        [MemberNotNull(nameof(_widthTextLbl), nameof(_heightTextLbl), nameof(_widthContentLbl), nameof(_heightContentLbl))]
        [MemberNotNull(nameof(_formatTextLbl), nameof(_paletteTextLbl), nameof(_formatBox), nameof(_paletteBox))]
        [MemberNotNull(nameof(_imageBox), nameof(_indexedImageBox), nameof(_paletteView), nameof(_imgList))]
        private void InitializeComponent()
        {
            #region Controls

            _widthTextLbl = new Label(LocalizationResources.ImageLabelWidth);
            _heightTextLbl = new Label(LocalizationResources.ImageLabelHeight);
            _widthContentLbl = new Label();
            _heightContentLbl = new Label();

            _formatTextLbl = new Label(LocalizationResources.ImageLabelFormat);
            _paletteTextLbl = new Label(LocalizationResources.ImageLabelPalette);
            _formatBox = new ComboBox<int> { MaxShowItems = 10, ShowBorder = true };
            _paletteBox = new ComboBox<int> { MaxShowItems = 10 };

            _imageBox = new ZoomablePictureBox { ShowBorder = true };
            _indexedImageBox = new ZoomableIndexedPictureBox { ShowBorder = true };
            _paletteView = new PaletteView { Spacing = new Vector2(2, 2), ShowBorder = true };

            _imgList = new global::ImGui.Forms.Controls.Lists.List<ImageThumbnail>
            {
                ItemSpacing = 4,
                IsSelectable = true,
                Size = Size.Parent
            };

            _saveBtn = new ImageButton
            {
                Image = ImageResources.Save,
                Tooltip = LocalizationResources.MenuFileSave,
                ImageSize = new Vector2(16, 16),
                Padding = new Vector2(5, 5),
                Enabled = false,
                KeyAction = new KeyCommand(ImGuiKey.ModCtrl, ImGuiKey.S, LocalizationResources.MenuFileSaveShortcut)
            };
            _saveAsBtn = new ImageButton
            {
                Image = ImageResources.SaveAs,
                Tooltip = LocalizationResources.MenuFileSaveAs,
                ImageSize = new Vector2(16, 16),
                Padding = new Vector2(5, 5),
                Enabled = false,
                KeyAction = new KeyCommand(ImGuiKey.F12, LocalizationResources.MenuFileSaveAsShortcut)
            };
            _imgExportBtn = new ImageButton { Image = ImageResources.ImageExport, Tooltip = LocalizationResources.ImageMenuExport, ImageSize = new Vector2(16, 16), Padding = new Vector2(5, 5) };
            _imgImportBtn = new ImageButton { Image = ImageResources.ImageImport, Tooltip = LocalizationResources.ImageMenuImport, ImageSize = new Vector2(16, 16), Padding = new Vector2(5, 5) };
            _batchImgExportBtn = new ImageButton { Image = ImageResources.BatchImageExport, Tooltip = LocalizationResources.ImageMenuExportBatch, ImageSize = new Vector2(16, 16), Padding = new Vector2(5, 5) };
            _batchImgImportBtn = new ImageButton { Image = ImageResources.BatchImageImport, Tooltip = LocalizationResources.ImageMenuImportBatch, ImageSize = new Vector2(16, 16), Padding = new Vector2(5, 5) };

            #endregion

            _listPaletteLayout = new StackLayout
            {
                Alignment = Alignment.Vertical,
                ItemSpacing = 4,
                Size = new Size(300, SizeValue.Parent),
                Items =
                {
                    _imgList
                }
            };

            _imageInfoLayout = new StackLayout
            {
                Alignment = Alignment.Vertical,
                ItemSpacing = 4,
                Size = Size.Parent,
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
                            _saveAsBtn,
                            new Splitter { Length = 26, Alignment = Alignment.Vertical },
                            _imgExportBtn,
                            _imgImportBtn,
                            new Splitter { Length = 26, Alignment = Alignment.Vertical },
                            _batchImgExportBtn,
                            _batchImgImportBtn
                        }
                    },
                    _imageBox,
                    new TableLayout
                    {
                        Spacing = new Vector2(4, 4),
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
            };

            _mainLayout = new StackLayout
            {
                Alignment = Alignment.Horizontal,
                ItemSpacing = 4,
                Items =
                {
                    _imageInfoLayout,
                    _listPaletteLayout
                }
            };
        }

        private void SetImages(IReadOnlyList<IImageFile> images)
        {
            _imgList.Items.Clear();
            _imgList.SelectedItem = null;

            if (images.Count <= 0)
                return;

            for (var i = 0; i < images.Count; i++)
            {
                var img = images[i];

                _imgList.Items.Add(new ImageThumbnail(img, i, img.GetImage()));
            }
        }

        private void SetSelectedImage(IImageFile img, IProgressContext progress)
        {
            SetFormats(img);
            SetPaletteFormats(img);

            _imgList.SelectedItem = _imgList.Items.FirstOrDefault(x => x.ImageFile == img);
            SetImage(img, progress);

            SetPalette(img, progress);

            _widthContentLbl.Text = img.ImageInfo.ImageSize.Width.ToString();
            _heightContentLbl.Text = img.ImageInfo.ImageSize.Height.ToString();
        }

        private void SetImage(IImageFile img, IProgressContext progress)
        {
            var image = img.GetImage(progress);

            if (img.IsIndexed)
            {
                _imageInfoLayout.Items[1] = _indexedImageBox;

                _indexedImageBox.SetImage(ImageResource.FromImage(image));
                _imageBox.SetImage(null);
            }
            else
            {
                _imageInfoLayout.Items[1] = _imageBox;

                _indexedImageBox.SetImage(null);
                _imageBox.SetImage(ImageResource.FromImage(image));
            }

            _imgList.SelectedItem?.SetThumbnail(image);
        }

        private void SetPalette(IImageFile img, IProgressContext progress)
        {
            if (!img.IsIndexed)
            {
                if (_listPaletteLayout.Items.Count > 1)
                    _listPaletteLayout.Items.RemoveAt(1);

                return;
            }

            if (_listPaletteLayout.Items.Count <= 1)
                _listPaletteLayout.Items.Add(_paletteView);

            IList<Rgba32> palette = img.GetPalette(progress);
            _paletteView.Palette = palette;
        }

        private void SetFormats(IImageFile img)
        {
            _formatBox.Items.Clear();
            _formatBox.SelectedItem = null;

            var hasFormats = img.EncodingDefinition.ColorEncodings.Any() || img.EncodingDefinition.IndexEncodings.Any();
            _formatBox.Visible = _formatTextLbl.Visible = hasFormats;

            if (!hasFormats)
                return;

            if (img.EncodingDefinition.ColorEncodings.Any())
                foreach (var colorEnc in img.EncodingDefinition.ColorEncodings)
                    _formatBox.Items.Add(new DropDownItem<int>(colorEnc.Key, colorEnc.Value.FormatName));

            if (img.EncodingDefinition.IndexEncodings.Any())
                foreach (var indexEnc in img.EncodingDefinition.IndexEncodings)
                    _formatBox.Items.Add(new DropDownItem<int>(indexEnc.Key, indexEnc.Value.IndexEncoding.FormatName));

            _formatBox.SelectedItem = _formatBox.Items.FirstOrDefault(x => x.Content == img.ImageInfo.ImageFormat);
        }

        private void SetPaletteFormats(IImageFile img)
        {
            _paletteBox.Items.Clear();
            _paletteBox.SelectedItem = null;

            IndexEncodingDefinition? indexInfo = img.EncodingDefinition.GetIndexEncoding(img.ImageInfo.ImageFormat);

            var hasPalettes = indexInfo is { PaletteEncodingIndices.Count: > 0 };
            _paletteBox.Visible = _paletteTextLbl.Visible = hasPalettes;

            if (!hasPalettes)
                return;

            foreach (var paletteFormat in indexInfo!.PaletteEncodingIndices)
            {
                var paletteEncoding = img.EncodingDefinition.GetPaletteEncoding(paletteFormat);
                if (paletteEncoding == null)
                    continue;

                _paletteBox.Items.Add(new DropDownItem<int>(paletteFormat, paletteEncoding.FormatName));
            }

            _paletteBox.SelectedItem = _paletteBox.Items.FirstOrDefault(x => x.Content == img.ImageInfo.PaletteFormat);
        }
    }
}
