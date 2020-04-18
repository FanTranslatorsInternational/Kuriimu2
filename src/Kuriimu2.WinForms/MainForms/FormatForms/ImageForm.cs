using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Cyotek.Windows.Forms;
using Kanvas;
using Kontract;
using Kontract.Extensions;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Progress;
using Kontract.Kanvas;
using Kontract.Models.Image;
using Kontract.Models.IO;
using Kore.Utilities.Palettes;
using Kuriimu2.WinForms.MainForms.Interfaces;
using Kuriimu2.WinForms.Properties;

namespace Kuriimu2.WinForms.MainForms.FormatForms
{
    /// <summary>
    /// The view for image plugins.
    /// </summary>
    public partial class ImageForm : UserControl, IKuriimuForm
    {
        private const string ZoomText_ = "Zoom: {0}%";
        private const string ToolZoomText_ = "Tool: Zoom";
        private const string ToolPanText_ = "Tool: Pan";

        private const string AllFilesFilter_ = "All Files (*.*)|*.*";
        private const string PngFileFilter_ = "Portable Network Graphics (*.png)|*.png";
        private const string KuriimuPaletteFilter_ = "Kuriimu PaletteData (*.kpal)|*.kpal";
        private const string RiffPaletteFilter_ = "Microsoft RIFF PaletteData (*.pal)|*.pal";

        private const string ExportPngTitle_ = "Export Png...";
        private const string ImportPngTitle_ = "Import Png...";

        private const string ExportPaletteTitle_ = "Export palette...";
        private const string ImportPaletteTitle_ = "Import palette...";
        private const string PaletteCouldNotOpenMessage_ = "Could not open palette file.";
        private const string PaletteCouldNotSaveMessage_ = "Could not save palette file.";
        private const string InvalidPaletteFileTitle_ = "Invalid palette";

        private const string DefaultCatchTitle_ = "Exception catched";

        private readonly Dictionary<string, string> _stylesText = new Dictionary<string, string>
        {
            ["None"] = "None",
            ["FixedSingle"] = "Simple",
            ["FixedSingleDropShadow"] = "Drop Shadow",
            ["FixedSingleGlowShadow"] = "Glow Shadow"
        };

        private readonly Dictionary<string, string> _stylesImages = new Dictionary<string, string>
        {
            ["None"] = "menu_border_none",
            ["FixedSingle"] = "menu_border_simple",
            ["FixedSingleDropShadow"] = "menu_border_drop_shadow",
            ["FixedSingleGlowShadow"] = "menu_border_glow_shadow"
        };

        private readonly IStateInfo _stateInfo;
        private readonly IProgressContext _progressContext;
        private readonly KanvasImage[] _kanvasImages;

        private int _selectedImageIndex;

        private Image _thumbnailBackground;

        private bool _setIndexInImage;
        private bool _paletteChooseColor;
        private int _paletteChosenColorIndex = -1;

        // ReSharper disable once SuspiciousTypeConversion.Global
        private ISaveFiles SaveState => _stateInfo.State as ISaveFiles;
        private IImageState ImageState => _stateInfo.State as IImageState;

        private ImageInfo SelectedImageInfo => ImageState.Images[_selectedImageIndex];
        private IKanvasImage SelectedImage => _kanvasImages[_selectedImageIndex];

        private IDictionary<int, IColorEncoding> ColorEncodings =>
            ImageState.SupportedEncodings ?? new Dictionary<int, IColorEncoding>();
        private IDictionary<int, (IIndexEncoding Encoding, IList<int> PaletteEncodingIndices)> IndexEncodings =>
            ImageState.SupportedIndexEncodings ?? new Dictionary<int, (IIndexEncoding, IList<int>)>();
        private IDictionary<int, IColorEncoding> PaletteEncodings =>
            ImageState.SupportedPaletteEncodings ?? new Dictionary<int, IColorEncoding>();

        /// <inheritdoc />
        public Func<SaveTabEventArgs, Task<bool>> SaveFilesDelegate { get; set; }

        /// <inheritdoc />
        public Action<IStateInfo> UpdateTabDelegate { get; set; }

        /// <summary>
        /// Create a new instance of <see cref="ImageForm"/>.
        /// </summary>
        /// <param name="stateInfo">The loaded state for an image format.</param>
        /// <param name="progressContext">The progress context.</param>
        /// <exception cref="T:System.InvalidOperationException">If state is not an image state.</exception>
        public ImageForm(IStateInfo stateInfo, IProgressContext progressContext)
        {
            InitializeComponent();

            if (!(stateInfo.State is IImageState imageState))
                throw new InvalidOperationException($"This state is not an {nameof(IImageState)}.");

            ContractAssertions.IsNotNull(stateInfo, nameof(stateInfo));
            ContractAssertions.IsNotNull(imageState.Images, nameof(imageState.Images));

            // Check integrity of the image state
            CheckIntegrity(imageState);

            _stateInfo = stateInfo;
            _progressContext = progressContext;
            _kanvasImages = imageState.Images.Select(x => new KanvasImage(imageState, x)).ToArray();

            imbPreview.Image = _kanvasImages.FirstOrDefault()?.GetImage(progressContext);

            // Populate format dropdown
            PopulateFormatDropdown();

            // Populate palette format dropdown
            PopulatePaletteDropdown(imageState.Images[_selectedImageIndex]);

            // Populate border style drop down
            PopulateBorderStyleDropdown();

            // Update form elements
            UpdateForm();
            UpdatePreview();
            UpdateImageList();
        }

        private void CheckIntegrity(IImageState imageState)
        {
            // Check if any encodings are given
            if (imageState.SupportedEncodings == null && imageState.SupportedIndexEncodings == null)
                throw new InvalidOperationException("The plugin has no supported encodings defined.");
            if (imageState.SupportedIndexEncodings != null && imageState.SupportedPaletteEncodings == null)
                throw new InvalidOperationException("The plugin has no supported palette encodings defined.");

            // Check for ambiguous format values
            if (imageState.SupportedEncodings?.Keys.Any(x => imageState.SupportedIndexEncodings?.Keys.Contains(x) ?? false) ?? false)
                throw new InvalidOperationException($"Ambiguous image format identifiers in plugin {ImageState.GetType().FullName}.");

            // Check that all image infos contain supported image formats
            foreach (var image in imageState.Images)
            {
                var isColorEncoding = imageState.SupportedEncodings?.ContainsKey(image.ImageFormat) ?? false;
                var isIndexColorEncoding = imageState.SupportedIndexEncodings?.ContainsKey(image.ImageFormat) ?? false;
                if (!isColorEncoding && !isIndexColorEncoding)
                    throw new InvalidOperationException($"Image format {image.ImageFormat} is not supported by the plugin.");

                if (isIndexColorEncoding && !image.IsIndexed)
                    throw new InvalidOperationException($"The image format {image.ImageFormat} is indexed, but the image is not.");
                if (isColorEncoding && image.IsIndexed)
                    throw new InvalidOperationException($"The image format {image.ImageFormat} is not indexed, but the image is.");

                if (image.IsIndexed && imageState.SupportedIndexEncodings != null)
                {
                    var indexEncoding = imageState.SupportedIndexEncodings[image.ImageFormat];
                    if (indexEncoding.PaletteEncodingIndices != null &&
                        !indexEncoding.PaletteEncodingIndices.All(x => imageState.SupportedPaletteEncodings.ContainsKey(x)))
                        throw new InvalidOperationException($"The image format {image.ImageFormat} depends on palette encodings not supported by the plugin.");
                }
            }
        }

        #region Dropdown population

        private void PopulateFormatDropdown()
        {
            var items = new List<ToolStripItem>();

            if (ImageState.SupportedEncodings != null)
            {
                items.AddRange(ImageState.SupportedEncodings.Select(f => new ToolStripMenuItem
                {
                    Text = f.Value.FormatName,
                    Tag = f.Key,
                    Checked = f.Key == ImageState.Images[_selectedImageIndex].ImageFormat
                }));
            }

            if (ImageState.SupportedIndexEncodings != null)
            {
                items.AddRange(ImageState.SupportedIndexEncodings.Select(f => new ToolStripMenuItem
                {
                    Text = f.Value.Encoding.FormatName,
                    Tag = f.Key,
                    Checked = f.Key == ImageState.Images[_selectedImageIndex].ImageFormat
                }));
            }

            tsbFormat.DropDownItems.AddRange(items.ToArray());
            if (tsbFormat.DropDownItems.Count > 0)
                foreach (var tsb in tsbFormat.DropDownItems)
                    ((ToolStripMenuItem)tsb).Click += tsbFormat_Click;
        }

        private void PopulatePaletteDropdown(ImageInfo imageInfo)
        {
            tsbPalette.DropDownItems.Clear();

            if (!imageInfo.IsIndexed)
                return;

            var items = new List<ToolStripItem>();
            if (ImageState.SupportedPaletteEncodings != null)
            {
                var paletteEncodings = ImageState.SupportedPaletteEncodings;

                var indices = ImageState.SupportedIndexEncodings[imageInfo.ImageFormat].PaletteEncodingIndices;
                if (imageInfo.IsIndexed && indices != null)
                    paletteEncodings = ImageState.SupportedPaletteEncodings.Where(x => indices.Contains(x.Key))
                        .ToDictionary(x => x.Key, y => y.Value);

                items.AddRange(paletteEncodings.Select(f => new ToolStripMenuItem
                {
                    Text = f.Value.FormatName,
                    Tag = f.Key,
                    Checked = f.Key == ImageState.Images[_selectedImageIndex].ImageFormat
                }));
            }

            tsbPalette.DropDownItems.AddRange(items.ToArray());
            if (tsbPalette.DropDownItems.Count > 0)
                foreach (var tsb in tsbPalette.DropDownItems)
                    ((ToolStripMenuItem)tsb).Click += tsbPalette_Click;
        }

        private void PopulateBorderStyleDropdown()
        {
            var items = Enum.GetNames(typeof(ImageBoxBorderStyle))
                .Select(s => new ToolStripMenuItem
                {
                    Image = (Image)Resources.ResourceManager.GetObject(_stylesImages[s]),
                    Text = _stylesText[s],
                    Tag = s
                }).ToArray<ToolStripItem>();

            tsbImageBorderStyle.DropDownItems.AddRange(items);
            foreach (var tsb in tsbImageBorderStyle.DropDownItems)
                ((ToolStripMenuItem)tsb).Click += tsbImageBorderStyle_Click;
        }

        #endregion

        #region Events

        private void ImageForm_Load(object sender, EventArgs e)
        {
            Dock = DockStyle.Fill;
            Padding = new Padding(3);
        }

        private void tsbFormat_Click(object sender, EventArgs e)
        {
            var tsb = (ToolStripMenuItem)sender;

            var newImageFormat = (int)tsb.Tag;

            DisableImageControls();
            DisablePaletteControls();

            SelectedImage.TranscodeImage(newImageFormat);

            PopulatePaletteDropdown(SelectedImageInfo);

            UpdateForm();
            UpdateImageList();
            UpdatePreview();
        }

        private void tsbPalette_Click(object sender, EventArgs e)
        {
            var tsb = (ToolStripMenuItem)sender;

            var newPaletteFormat = (int)tsb.Tag;

            DisableImageControls();
            DisablePaletteControls();

            SelectedImage.TranscodePalette(newPaletteFormat);

            UpdateForm();
            UpdateImageList();
            UpdatePreview();
        }

        private void tsbGridColor1_Click(object sender, EventArgs e)
        {
            SetGridColor(imbPreview.GridColor, clr =>
            {
                imbPreview.GridColor = clr;
                Settings.Default.GridColor1 = clr;
                Settings.Default.Save();
            });
        }

        private void tsbGridColor2_Click(object sender, EventArgs e)
        {
            SetGridColor(imbPreview.GridColorAlternate, clr =>
            {
                imbPreview.GridColorAlternate = clr;
                Settings.Default.GridColor2 = clr;
                Settings.Default.Save();
            });
        }

        private void tsbImageBorderStyle_Click(object sender, EventArgs e)
        {
            var tsb = (ToolStripMenuItem)sender;
            var style = (ImageBoxBorderStyle)Enum.Parse(typeof(ImageBoxBorderStyle), tsb.Tag.ToString());

            imbPreview.ImageBorderStyle = style;
            Settings.Default.ImageBorderStyle = style;
            Settings.Default.Save();

            UpdatePreview();
        }

        private void tsbImageBorderColor_Click(object sender, EventArgs e)
        {
            clrDialog.Color = imbPreview.ImageBorderColor;
            if (clrDialog.ShowDialog() != DialogResult.OK)
                return;

            imbPreview.ImageBorderColor = clrDialog.Color;
            Settings.Default.ImageBorderColor = clrDialog.Color;
            Settings.Default.Save();

            UpdatePreview();
        }

        private void imbPreview_Zoomed(object sender, ImageBoxZoomEventArgs e)
        {
            tslZoom.Text = string.Format(ZoomText_, imbPreview.Zoom);
        }

        private void imbPreview_MouseEnter(object sender, EventArgs e)
        {
            imbPreview.Focus();
        }

        private void imbPreview_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                imbPreview.SelectionMode = ImageBoxSelectionMode.None;
                imbPreview.Cursor = Cursors.SizeAll;
                tslTool.Text = ToolPanText_;
            }

            if (e.KeyCode == Keys.ShiftKey)
            {
                _setIndexInImage = true;
            }
        }

        private void imbPreview_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                imbPreview.SelectionMode = ImageBoxSelectionMode.Zoom;
                imbPreview.Cursor = Cursors.Default;
                tslTool.Text = ToolZoomText_;
            }

            if (e.KeyCode == Keys.ShiftKey)
            {
                _setIndexInImage = false;
            }
        }

        private void treBitmaps_MouseEnter(object sender, EventArgs e)
        {
            treBitmaps.Focus();
        }

        private void treBitmaps_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (_selectedImageIndex != treBitmaps.SelectedNode.Index)
            {
                _selectedImageIndex = treBitmaps.SelectedNode.Index;
                UpdatePreview();
            }
        }

        private void tsbSave_Click(object sender, EventArgs e)
        {
            Save(UPath.Empty).ConfigureAwait(false);
        }

        private void tsbSaveAs_Click(object sender, EventArgs e)
        {
            SaveAs().ConfigureAwait(false);
        }

        private void tsbExport_Click(object sender, EventArgs e)
        {
            ExportPng();
        }

        private void tsbImport_Click(object sender, EventArgs e)
        {
            ImportPng();
        }

        private void imbPreview_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void imbPreview_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            if (files.Length > 0 && File.Exists(files[0]))
                Import(files[0]);
        }

        private async void imbPreview_MouseClick(object sender, MouseEventArgs e)
        {
            if (_setIndexInImage)
            {
                if (_paletteChosenColorIndex >= 0)
                    await SetIndexInImage(e.Location, _paletteChosenColorIndex);
                else
                    await SetColorInPalette(GetPaletteIndexByImageLocation, e.Location);

                _setIndexInImage = false;
            }
        }

        private void tsbPaletteImport_Click(object sender, EventArgs e)
        {
            ImportPalette();
        }

        private void tsbPaletteExport_Click(object sender, EventArgs e)
        {
            ExportPalette();
        }

        private async void pbPalette_MouseClick(object sender, MouseEventArgs e)
        {
            if (_paletteChooseColor)
            {
                if (e.Button.HasFlag(MouseButtons.Right))
                {
                    _paletteChooseColor = false;
                    _paletteChosenColorIndex = -1;
                }
                else if (e.Button.HasFlag(MouseButtons.Left))
                {
                    _paletteChosenColorIndex = GetPaletteIndex(e.Location);
                }

                UpdatePaletteImage();
            }
            else
            {
                await SetColorInPalette(GetPaletteIndex, e.Location);
            }
        }

        private void pbPalette_MouseEnter(object sender, EventArgs e)
        {
            pbPalette.Focus();
        }

        private void pbPalette_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ShiftKey)
                _paletteChooseColor = true;
        }

        private void pbPalette_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ShiftKey)
                _paletteChooseColor = false;
        }

        #endregion

        #region Save

        private async Task SaveAs()
        {
            var sfd = new SaveFileDialog
            {
                FileName = _stateInfo.FilePath.GetName(),
                Filter = AllFilesFilter_
            };

            if (sfd.ShowDialog() == DialogResult.OK)
                await Save(sfd.FileName);
        }

        private async Task Save(UPath savePath)
        {
            if (savePath == UPath.Empty)
                savePath = _stateInfo.FileSystem.ConvertPathToInternal(UPath.Root) / _stateInfo.FilePath;

            var result = await SaveFilesDelegate(new SaveTabEventArgs(_stateInfo, savePath));

            if (result && SaveState != null)
                SaveState.ContentChanged = false;

            UpdateForm();
        }

        private void ExportPng()
        {
            var sfd = new SaveFileDialog
            {
                Title = ExportPngTitle_,
                InitialDirectory = Settings.Default.LastDirectory,
                FileName = _stateInfo.FilePath.GetNameWithoutExtension() + "." + _selectedImageIndex.ToString("00") + ".png",
                Filter = PngFileFilter_,
                AddExtension = true
            };

            if (sfd.ShowDialog() == DialogResult.OK)
                SelectedImage.GetImage(_progressContext).Save(sfd.FileName, ImageFormat.Png);
        }

        #endregion

        #region Import

        private void ImportPng()
        {
            var ofd = new OpenFileDialog
            {
                Title = ImportPngTitle_,
                InitialDirectory = Settings.Default.LastDirectory,
                Filter = PngFileFilter_
            };

            if (ofd.ShowDialog() == DialogResult.OK)
                Import(ofd.FileName);
        }

        private void Import(UPath filePath)
        {
            try
            {
                var newImage = new Bitmap(filePath.FullName);
                SelectedImage.SetImage(newImage, _progressContext);

                treBitmaps.SelectedNode = treBitmaps.Nodes[_selectedImageIndex];
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            UpdateForm();
            UpdateImageList();
            UpdatePreview();
        }

        #endregion

        #region Update

        /// <inheritdoc />
        public void UpdateForm()
        {
            tsbSave.Enabled = ImageState is ISaveFiles;
            tsbSaveAs.Enabled = ImageState is ISaveFiles && _stateInfo.ParentStateInfo == null;

            var isIndexed = SelectedImageInfo.IsIndexed;
            tslPalette.Visible = isIndexed;
            tsbPalette.Visible = isIndexed;
            tsbPalette.Enabled = isIndexed && PaletteEncodings.Any();
            pbPalette.Enabled = isIndexed;
            tsbPaletteImport.Enabled = isIndexed;

            splProperties.Panel2Collapsed = !isIndexed;

            tsbFormat.Enabled = ColorEncodings.Any() || IndexEncodings.Any();

            imbPreview.Enabled = ImageState.Images.Any();

            UpdateTabDelegate?.Invoke(_stateInfo);
        }

        private void UpdatePreview()
        {
            if (ImageState.Images.Count > 0)
            {
                var horizontalScroll = imbPreview.HorizontalScroll.Value;
                var verticalScroll = imbPreview.VerticalScroll.Value;

                imbPreview.Image = SelectedImage.GetImage(_progressContext);
                imbPreview.Zoom -= 1;
                imbPreview.Zoom += 1;

                imbPreview.ScrollTo(horizontalScroll, verticalScroll);
            }

            // Grid Color 1
            imbPreview.GridColor = Settings.Default.GridColor1;
            var gc1Bitmap = new Bitmap(16, 16, PixelFormat.Format24bppRgb);
            var gfx = Graphics.FromImage(gc1Bitmap);
            gfx.FillRectangle(new SolidBrush(Settings.Default.GridColor1), 0, 0, 16, 16);
            tsbGridColor1.Image = gc1Bitmap;

            // Grid Color 2
            imbPreview.GridColorAlternate = Settings.Default.GridColor2;
            var gc2Bitmap = new Bitmap(16, 16, PixelFormat.Format24bppRgb);
            gfx = Graphics.FromImage(gc2Bitmap);
            gfx.FillRectangle(new SolidBrush(Settings.Default.GridColor2), 0, 0, 16, 16);
            tsbGridColor2.Image = gc2Bitmap;

            // Image Border Style
            imbPreview.ImageBorderStyle = Settings.Default.ImageBorderStyle;
            tsbImageBorderStyle.Image = (Image)Resources.ResourceManager.GetObject(_stylesImages[Settings.Default.ImageBorderStyle.ToString()]);
            tsbImageBorderStyle.Text = _stylesText[Settings.Default.ImageBorderStyle.ToString()];

            // Image Border Color
            imbPreview.ImageBorderColor = Settings.Default.ImageBorderColor;
            var ibcBitmap = new Bitmap(16, 16, PixelFormat.Format24bppRgb);
            gfx = Graphics.FromImage(ibcBitmap);
            gfx.FillRectangle(new SolidBrush(Settings.Default.ImageBorderColor), 0, 0, 16, 16);
            tsbImageBorderColor.Image = ibcBitmap;

            // Format Dropdown
            tsbFormat.Text = ColorEncodings.ContainsKey(SelectedImageInfo.ImageFormat) ?
                ColorEncodings[SelectedImageInfo.ImageFormat].FormatName :
                IndexEncodings[SelectedImageInfo.ImageFormat].Encoding.FormatName;
            tsbFormat.Tag = SelectedImageInfo.ImageFormat;

            // Update selected format
            foreach (ToolStripMenuItem tsm in tsbFormat.DropDownItems)
                tsm.Checked = (int)tsm.Tag == SelectedImageInfo.ImageFormat;

            UpdatePaletteImage();

            // Dimensions
            tslWidth.Text = SelectedImageInfo.ImageSize.Width.ToString();
            tslHeight.Text = SelectedImageInfo.ImageSize.Height.ToString();
        }

        private void UpdatePaletteImage()
        {
            if (!SelectedImageInfo.IsIndexed)
                return;

            // PaletteData Dropdown
            tsbPalette.Text = PaletteEncodings[SelectedImageInfo.PaletteFormat].FormatName;
            tsbPalette.Tag = SelectedImageInfo.PaletteFormat;

            // Update selected palette format
            foreach (ToolStripMenuItem tsm in tsbPalette.DropDownItems)
                tsm.Checked = (int)tsm.Tag == SelectedImageInfo.PaletteFormat;

            // PaletteData Picture Box
            var palette = SelectedImage.GetPalette(_progressContext);
            var dimPalette = (int)Math.Ceiling(Math.Sqrt(palette.Count));
            var paletteImg = palette.ToBitmap(new Size(dimPalette, dimPalette));
            if (paletteImg != null)
                pbPalette.Image = paletteImg;
        }

        private void UpdateImageList()
        {
            if (ImageState.Images.Count <= 0)
                return;

            var selectedIndex = treBitmaps.SelectedNode?.Index ?? -1;

            treBitmaps.BeginUpdate();
            treBitmaps.Nodes.Clear();
            imlBitmaps.Images.Clear();
            imlBitmaps.TransparentColor = Color.Transparent;
            imlBitmaps.ImageSize = new Size(Settings.Default.ThumbnailWidth, Settings.Default.ThumbnailHeight);
            treBitmaps.ItemHeight = Settings.Default.ThumbnailHeight + 6;

            for (var i = 0; i < ImageState.Images.Count; i++)
            {
                imlBitmaps.Images.Add(i.ToString(), GenerateThumbnail(_kanvasImages[i].GetImage(_progressContext)));
                var treeNode = new TreeNode
                {
                    Text = !string.IsNullOrEmpty(ImageState.Images[i].Name)
                        ? ImageState.Images[i].Name
                        : i.ToString("00"),
                    Tag = i,
                    ImageKey = i.ToString(),
                    SelectedImageKey = i.ToString()
                };

                treBitmaps.Nodes.Add(treeNode);
                if (i == selectedIndex)
                    treBitmaps.SelectedNode = treeNode;
            }

            treBitmaps.EndUpdate();
        }

        #endregion

        #region Private methods

        private void SetGridColor(Color startColor, Action<Color> setColorToProperties)
        {
            clrDialog.Color = startColor;
            if (clrDialog.ShowDialog() != DialogResult.OK)
                return;

            setColorToProperties(clrDialog.Color);

            UpdatePreview();
            GenerateThumbnailBackground();
            UpdateImageList();

            treBitmaps.SelectedNode = treBitmaps.Nodes[_selectedImageIndex];
        }

        private void GenerateThumbnailBackground()
        {
            var thumbWidth = Settings.Default.ThumbnailWidth;
            var thumbHeight = Settings.Default.ThumbnailHeight;
            var thumb = new Bitmap(thumbWidth, thumbHeight, PixelFormat.Format24bppRgb);
            var gfx = Graphics.FromImage(thumb);

            // Grid
            var xCount = Settings.Default.ThumbnailWidth / 16 + 1;
            var yCount = Settings.Default.ThumbnailHeight / 16 + 1;

            gfx.FillRectangle(new SolidBrush(Settings.Default.GridColor1), 0, 0, thumbWidth, thumbHeight);
            for (var i = 0; i < xCount; i++)
                for (var j = 0; j < yCount; j++)
                    if ((i + j) % 2 != 1)
                        gfx.FillRectangle(new SolidBrush(Settings.Default.GridColor2), i * 16, j * 16, 16, 16);

            _thumbnailBackground = thumb;
        }

        private Bitmap GenerateThumbnail(Image input)
        {
            var thumbWidth = Settings.Default.ThumbnailWidth;
            var thumbHeight = Settings.Default.ThumbnailHeight;
            var thumb = new Bitmap(thumbWidth, thumbHeight, PixelFormat.Format24bppRgb);
            var gfx = Graphics.FromImage(thumb);

            gfx.CompositingQuality = CompositingQuality.HighSpeed;
            gfx.PixelOffsetMode = PixelOffsetMode.Default;
            gfx.SmoothingMode = SmoothingMode.HighSpeed;
            gfx.InterpolationMode = InterpolationMode.Default;

            var wRatio = (float)input.Width / thumbWidth;
            var hRatio = (float)input.Height / thumbHeight;
            var ratio = wRatio >= hRatio ? wRatio : hRatio;

            if (input.Width <= thumbWidth && input.Height <= thumbHeight)
                ratio = 1.0f;

            var size = new Size((int)Math.Min(input.Width / ratio, thumbWidth), (int)Math.Min(input.Height / ratio, thumbHeight));
            var pos = new Point(thumbWidth / 2 - size.Width / 2, thumbHeight / 2 - size.Height / 2);

            // Grid
            if (_thumbnailBackground == null)
                GenerateThumbnailBackground();

            gfx.DrawImageUnscaled(_thumbnailBackground, 0, 0, _thumbnailBackground.Width, _thumbnailBackground.Height);
            gfx.InterpolationMode = ratio != 1.0f ? InterpolationMode.HighQualityBicubic : InterpolationMode.Default;
            gfx.DrawImage(input, pos.X, pos.Y, size.Width, size.Height);

            return thumb;
        }

        private async Task SetColorInPalette(Func<Point, int> indexFunc, Point controlPoint)
        {
            if (!SelectedImageInfo.IsIndexed)
                return;

            DisablePaletteControls();
            DisableImageControls();

            var index = indexFunc(controlPoint);
            if (index < 0 || index >= SelectedImage.GetPalette(_progressContext).Count)
            {
                UpdateForm();
                return;
            }

            if (clrDialog.ShowDialog() != DialogResult.OK)
            {
                UpdateForm();
                return;
            }

            try
            {
                await Task.Run(() =>
                {
                    SelectedImage.SetColorInPalette(index, clrDialog.Color);

                    _progressContext.ReportProgress("Done", 1, 1);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), DefaultCatchTitle_, MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateForm();
                return;
            }

            if (SaveState != null)
                SaveState.ContentChanged = true;

            UpdateForm();
            UpdatePreview();
            UpdateImageList();
        }

        private async Task SetIndexInImage(Point controlPoint, int newIndex)
        {
            if (!SelectedImageInfo.IsIndexed)
                return;

            if (newIndex >= SelectedImage.GetPalette(_progressContext).Count)
                return;

            DisablePaletteControls();
            DisableImageControls();

            var pointInImg = GetPointInImage(controlPoint);
            if (pointInImg == Point.Empty)
            {
                UpdateForm();
                return;
            }

            try
            {
                await Task.Run(() =>
                {
                    SelectedImage.SetIndexInImage(pointInImg, newIndex);

                    _progressContext.ReportProgress("Done", 1, 1);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), DefaultCatchTitle_, MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateForm();
                return;
            }

            if (SaveState != null)
                SaveState.ContentChanged = true;

            UpdateForm();
            UpdatePreview();
            UpdateImageList();
        }

        private void DisablePaletteControls()
        {
            pbPalette.Enabled = false;
            tsbPalette.Enabled = false;
        }

        private void DisableImageControls()
        {
            imbPreview.Enabled = false;
            tsbFormat.Enabled = false;
        }

        private int GetPaletteIndex(Point point)
        {
            var xIndex = point.X / (pbPalette.Width / pbPalette.Image.Width);
            var yIndex = point.Y / (pbPalette.Height / pbPalette.Image.Height);
            return yIndex * pbPalette.Image.Width + xIndex;
        }

        private Point GetPointInImage(Point controlPoint)
        {
            return imbPreview.IsPointInImage(controlPoint) ?
                imbPreview.PointToImage(controlPoint) :
                Point.Empty;
        }

        private int GetPaletteIndexByImageLocation(Point point)
        {
            var pointInImg = GetPointInImage(point);
            if (pointInImg == Point.Empty)
                return -1;

            var pixelColor = SelectedImage.GetImage(_progressContext).GetPixel(pointInImg.X, pointInImg.Y).ToArgb();

            var palette = SelectedImage.GetPalette(_progressContext);
            for (var i = 0; i < palette.Count; i++)
                if (palette[i].ToArgb() == pixelColor)
                    return i;

            return -1;
        }

        private void ImportPalette()
        {
            if (SelectedImageInfo.IsIndexed)
                return;

            var colors = LoadPaletteFile();
            if (colors == null)
                return;

            DisablePaletteControls();
            DisableImageControls();

            try
            {
                SelectedImage.SetPalette(colors, _progressContext);

                _progressContext.ReportProgress("Done", 1, 1);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), DefaultCatchTitle_, MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateForm();
                return;
            }

            if (SaveState != null)
                SaveState.ContentChanged = true;

            UpdateForm();
            UpdatePreview();
            UpdateImageList();
        }

        private IList<Color> LoadPaletteFile()
        {
            var ofd = new OpenFileDialog
            {
                Title = ImportPaletteTitle_,
                InitialDirectory = Settings.Default.LastDirectory,
                Filter = string.Join("|", KuriimuPaletteFilter_, RiffPaletteFilter_)
            };

            if (ofd.ShowDialog() != DialogResult.OK)
            {
                MessageBox.Show(PaletteCouldNotOpenMessage_, InvalidPaletteFileTitle_, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return null;
            }

            IList<Color> palette = null;
            if (Path.GetExtension(ofd.FileName) == ".kpal")
            {
                try
                {
                    var kpal = KPal.FromFile(ofd.FileName);
                    palette = kpal.Palette;
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), DefaultCatchTitle_, MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            else if (Path.GetExtension(ofd.FileName) == ".pal")
            {
                try
                {
                    var pal = RiffPal.FromFile(ofd.FileName);
                    palette = pal.Palette;
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), DefaultCatchTitle_, MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }

            return palette;
        }

        private void ExportPalette()
        {
            if (!SelectedImageInfo.IsIndexed)
                return;

            SavePaletteFile(SelectedImage.GetPalette(_progressContext));
        }

        private void SavePaletteFile(IList<Color> colors)
        {
            var sfd = new SaveFileDialog
            {
                Title = ExportPaletteTitle_,
                InitialDirectory = Settings.Default.LastDirectory,
                Filter = KuriimuPaletteFilter_
            };

            if (sfd.ShowDialog() != DialogResult.OK)
            {
                MessageBox.Show(PaletteCouldNotSaveMessage_, InvalidPaletteFileTitle_, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            try
            {
                var kpal = new KPal(colors, 1);
                kpal.Save(sfd.FileName);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), DefaultCatchTitle_, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        #endregion

        private void pbPalette_Paint(object sender, PaintEventArgs e)
        {
            var palette = SelectedImage.GetPalette(_progressContext);
            if (_paletteChosenColorIndex >= 0 &&
                _paletteChosenColorIndex < palette.Count)
            {
                var dimPalette = (int)Math.Ceiling(Math.Sqrt(palette.Count));

                var colorOnIndex = palette[_paletteChosenColorIndex];
                var brushColor = colorOnIndex.GetBrightness() <= 0.49 ? Color.White : Color.Black;

                var width = pbPalette.Width / dimPalette;
                var height = pbPalette.Height / dimPalette;
                var rect = new Rectangle(_paletteChosenColorIndex % dimPalette * width, _paletteChosenColorIndex / dimPalette * height,
                    width, height);

                e.Graphics.DrawRectangle(new Pen(new SolidBrush(brushColor), 3), rect);
            }
        }
    }
}
