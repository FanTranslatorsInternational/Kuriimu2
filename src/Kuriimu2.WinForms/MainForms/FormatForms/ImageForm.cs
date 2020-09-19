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

        private const string PngFileFilter_ = "Portable Network Graphics (*.png)|*.png";
        private const string KuriimuPaletteFilter_ = "Kuriimu PaletteData (*.kpal)|*.kpal";
        private const string RiffPaletteFilter_ = "Microsoft RIFF PaletteData (*.pal)|*.pal";

        private const string ExportPngTitle_ = "Export Png...";
        private const string ImportPngTitle_ = "Import Png...";

        private const string ExportPaletteTitle_ = "Export palette...";
        private const string ImportPaletteTitle_ = "Import palette...";
        private const string PaletteCouldNotOpenMessage_ = "Could not open palette file.";
        private const string PaletteCouldNotSaveMessage_ = "Could not save palette file.";

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
        private readonly IFormCommunicator _formCommunicator;

        private int _selectedImageIndex;

        private Image _thumbnailBackground;

        private bool _setIndexInImage;
        private bool _paletteChooseColor;
        private int _paletteChosenColorIndex = -1;

        private IImageState ImageState => _stateInfo.PluginState as IImageState;

        private EncodingDefinition EncodingDefinition => ImageState.EncodingDefinition;
        private IKanvasImage SelectedImage => ImageState.Images[_selectedImageIndex];

        /// <summary>
        /// Create a new instance of <see cref="ImageForm"/>.
        /// </summary>
        /// <param name="stateInfo">The loaded state for an image format.</param>
        /// <param name="formCommunicator"><see cref="IFormCommunicator"/> to allow communication with the main form.</param>
        /// <param name="progressContext">The progress context.</param>
        /// <exception cref="T:System.InvalidOperationException">If state is not an image state.</exception>
        public ImageForm(IStateInfo stateInfo, IFormCommunicator formCommunicator, IProgressContext progressContext)
        {
            InitializeComponent();

            if (!(stateInfo.PluginState is IImageState imageState))
                throw new InvalidOperationException($"This state is not an {nameof(IImageState)}.");

            ContractAssertions.IsNotNull(stateInfo, nameof(stateInfo));
            ContractAssertions.IsNotNull(imageState.Images, nameof(imageState.Images));

            // Check integrity of the image state
            CheckIntegrity(imageState);

            _stateInfo = stateInfo;
            _formCommunicator = formCommunicator;
            _progressContext = progressContext;

            imbPreview.Image = ImageState.Images.FirstOrDefault()?.GetImage(progressContext);

            // Populate format dropdown
            PopulateFormatDropdown();

            // Populate palette format dropdown
            PopulatePaletteDropdown(imageState.Images[_selectedImageIndex]);

            // Populate border style drop down
            PopulateBorderStyleDropdown();

            // Update form elements
            UpdateFormInternal();
            UpdatePreview();
            UpdateImageList();
        }

        private void CheckIntegrity(IImageState imageState)
        {
            var encodingDefinition = imageState.EncodingDefinition;

            // Check if any encodings are given
            if (!encodingDefinition.HasColorEncodings && !encodingDefinition.HasIndexEncodings)
                throw new InvalidOperationException("The plugin has no supported encodings defined.");
            if (encodingDefinition.HasIndexEncodings && !encodingDefinition.HasPaletteEncodings)
                throw new InvalidOperationException("The plugin has no supported palette encodings defined.");
        }

        #region Dropdown population

        private void PopulateFormatDropdown()
        {
            var items = new List<ToolStripItem>();

            if (EncodingDefinition.HasColorEncodings)
            {
                items.AddRange(EncodingDefinition.ColorEncodings.Select(f => new ToolStripMenuItem
                {
                    Text = f.Value.FormatName,
                    Tag = f.Key,
                    Checked = f.Key == ImageState.Images[_selectedImageIndex].ImageFormat
                }));
            }

            if (EncodingDefinition.HasIndexEncodings)
            {
                items.AddRange(EncodingDefinition.IndexEncodings.Select(f => new ToolStripMenuItem
                {
                    Text = f.Value.IndexEncoding.FormatName,
                    Tag = f.Key,
                    Checked = f.Key == ImageState.Images[_selectedImageIndex].ImageFormat
                }));
            }

            tsbFormat.DropDownItems.AddRange(items.ToArray());
            if (tsbFormat.DropDownItems.Count <= 0)
                return;

            foreach (var tsb in tsbFormat.DropDownItems)
                ((ToolStripMenuItem)tsb).Click += tsbFormat_Click;
        }

        private void PopulatePaletteDropdown(IKanvasImage image)
        {
            tsbPalette.DropDownItems.Clear();

            if (!image.IsIndexed)
                return;

            var items = new List<ToolStripItem>();
            if (EncodingDefinition.HasPaletteEncodings)
            {
                var paletteEncodings = EncodingDefinition.PaletteEncodings;

                var indices = EncodingDefinition.GetIndexEncoding(image.ImageFormat).PaletteEncodingIndices;
                if (image.IsIndexed && indices.Any())
                    paletteEncodings = EncodingDefinition.PaletteEncodings.Where(x => indices.Contains(x.Key))
                        .ToDictionary(x => x.Key, y => y.Value);

                items.AddRange(paletteEncodings.Select(f => new ToolStripMenuItem
                {
                    Text = f.Value.FormatName,
                    Tag = f.Key,
                    Checked = f.Key == ImageState.Images[_selectedImageIndex].ImageFormat
                }));
            }

            tsbPalette.DropDownItems.AddRange(items.ToArray());
            if (tsbPalette.DropDownItems.Count <= 0)
                return;

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

            SelectedImage.TranscodeImage(newImageFormat, _progressContext);

            PopulatePaletteDropdown(SelectedImage);

            UpdateFormInternal();
            UpdateImageList();
            UpdatePreview();
        }

        private void tsbPalette_Click(object sender, EventArgs e)
        {
            var tsb = (ToolStripMenuItem)sender;

            var newPaletteFormat = (int)tsb.Tag;

            DisableImageControls();
            DisablePaletteControls();

            SelectedImage.TranscodePalette(newPaletteFormat, _progressContext);

            UpdateFormInternal();
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
            Save();
        }

        private void tsbSaveAs_Click(object sender, EventArgs e)
        {
            SaveAs();
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

        private void SaveAs()
        {
            Save(true);
        }

        private async void Save(bool saveAs = false)
        {
            var wasSuccessful = await _formCommunicator.Save(saveAs);
            if (!wasSuccessful)
                return;

            UpdateProperties();
            _formCommunicator.Update(true, false);
        }

        #endregion

        #region Export

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

            if (sfd.ShowDialog() != DialogResult.OK)
                return;

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

            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            Import(ofd.FileName);
        }

        private void Import(UPath filePath)
        {
            try
            {
                var newImage = new Bitmap(filePath.FullName);
                SelectedImage.SetImage(newImage, _progressContext);
                newImage.Dispose();

                treBitmaps.SelectedNode = treBitmaps.Nodes[_selectedImageIndex];
            }
            catch (Exception ex)
            {
                _formCommunicator.ReportStatus(false, ex.Message);
            }

            UpdateFormInternal();

            UpdateImageList();
            UpdatePreview();
        }

        #endregion

        #region Update

        /// <inheritdoc />
        public void UpdateForm()
        {
            UpdateProperties();
        }

        private void UpdateFormInternal()
        {
            UpdateProperties();
            _formCommunicator.Update(true, false);
        }

        private void UpdateProperties()
        {
            tsbSave.Enabled = ImageState is ISaveFiles;
            tsbSaveAs.Enabled = ImageState is ISaveFiles && _stateInfo.ParentStateInfo == null;

            var isIndexed = SelectedImage.IsIndexed;
            tslPalette.Visible = isIndexed;
            tsbPalette.Visible = isIndexed;
            tsbPalette.Enabled = isIndexed && EncodingDefinition.HasPaletteEncodings;
            pbPalette.Enabled = isIndexed;
            tsbPaletteImport.Enabled = isIndexed;

            splProperties.Panel2Collapsed = !isIndexed;

            tsbFormat.Enabled = EncodingDefinition.HasColorEncodings || EncodingDefinition.HasIndexEncodings;

            imbPreview.Enabled = ImageState.Images.Any();
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
            tsbFormat.Text = EncodingDefinition.GetColorEncoding(SelectedImage.ImageFormat)?.FormatName ??
                             EncodingDefinition.GetIndexEncoding(SelectedImage.ImageFormat).IndexEncoding.FormatName;
            tsbFormat.Tag = SelectedImage.ImageFormat;

            // Update selected format
            foreach (ToolStripMenuItem tsm in tsbFormat.DropDownItems)
                tsm.Checked = (int)tsm.Tag == SelectedImage.ImageFormat;

            UpdatePaletteImage();

            // Dimensions
            tslWidth.Text = SelectedImage.ImageSize.Width.ToString();
            tslHeight.Text = SelectedImage.ImageSize.Height.ToString();
        }

        private void UpdatePaletteImage()
        {
            if (!SelectedImage.IsIndexed)
                return;

            // PaletteData Dropdown
            tsbPalette.Text = EncodingDefinition.GetPaletteEncoding(SelectedImage.PaletteFormat).FormatName;
            tsbPalette.Tag = SelectedImage.PaletteFormat;

            // Update selected palette format
            foreach (ToolStripMenuItem tsm in tsbPalette.DropDownItems)
                tsm.Checked = (int)tsm.Tag == SelectedImage.PaletteFormat;

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
                imlBitmaps.Images.Add(i.ToString(), GenerateThumbnail(ImageState.Images[i].GetImage(_progressContext)));
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
            if (!SelectedImage.IsIndexed)
                return;

            var index = indexFunc(controlPoint);
            if (index < 0 || index >= SelectedImage.GetPalette(_progressContext).Count)
                return;

            if (clrDialog.ShowDialog() != DialogResult.OK)
                return;

            DisablePaletteControls();
            DisableImageControls();

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
                _formCommunicator.ReportStatus(false, ex.Message);
                UpdateFormInternal();
                return;
            }

            UpdateFormInternal();

            UpdatePreview();
            UpdateImageList();
        }

        private async Task SetIndexInImage(Point controlPoint, int newIndex)
        {
            if (!SelectedImage.IsIndexed)
                return;

            if (newIndex >= SelectedImage.GetPalette(_progressContext).Count)
                return;

            var pointInImg = GetPointInImage(controlPoint);
            if (pointInImg == Point.Empty)
                return;

            DisablePaletteControls();
            DisableImageControls();

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
                _formCommunicator.ReportStatus(false, ex.Message);
                UpdateFormInternal();
                return;
            }

            UpdateFormInternal();

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
            if (SelectedImage.IsIndexed)
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
                _formCommunicator.ReportStatus(false, ex.Message);
                UpdateFormInternal();
                return;
            }

            UpdateFormInternal();
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
                _formCommunicator.ReportStatus(false, PaletteCouldNotOpenMessage_);
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
                    _formCommunicator.ReportStatus(false, e.Message);
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
                    _formCommunicator.ReportStatus(false, e.Message);
                }
            }

            return palette;
        }

        private void ExportPalette()
        {
            if (!SelectedImage.IsIndexed)
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
                _formCommunicator.ReportStatus(false, PaletteCouldNotSaveMessage_);
                return;
            }

            try
            {
                var kpal = new KPal(colors, 1);
                kpal.Save(sfd.FileName);
            }
            catch (Exception e)
            {
                _formCommunicator.ReportStatus(false, e.Message);
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
