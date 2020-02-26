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
using Kontract.Models.Images;
using Kontract.Models.IO;
using Kore.Utilities.Palettes;
using Kuriimu2.WinForms.Interfaces;
using Kuriimu2.WinForms.Properties;

namespace Kuriimu2.WinForms.FormatForms
{
    public partial class ImageForm : UserControl, IKuriimuForm
    {
        readonly Dictionary<string, string> _stylesText = new Dictionary<string, string>
        {
            ["None"] = "None",
            ["FixedSingle"] = "Simple",
            ["FixedSingleDropShadow"] = "Drop Shadow",
            ["FixedSingleGlowShadow"] = "Glow Shadow"
        };

        readonly Dictionary<string, string> _stylesImages = new Dictionary<string, string>
        {
            ["None"] = "menu_border_none",
            ["FixedSingle"] = "menu_border_simple",
            ["FixedSingleDropShadow"] = "menu_border_drop_shadow",
            ["FixedSingleGlowShadow"] = "menu_border_glow_shadow"
        };

        private readonly IStateInfo _stateInfo;
        private readonly IImageState _imageState;

        private readonly IList<Bitmap> _images;
        private readonly IList<Bitmap> _bestImages;
        private readonly IList<IList<Color>> _imagePalettes;
        private int _selectedImageIndex;

        private Image _thumbnailBackground;

        private bool _setIndexInImage;
        private bool _paletteChooseColor;
        private int _paletteChosenColorIndex = -1;

        private ImageInfo SelectedImageInfo => _imageState.Images[_selectedImageIndex];
        private int SelectedImageFormat => SelectedImageInfo.ImageFormat;
        private int SelectedPaletteFormat => (SelectedImageInfo as IndexImageInfo)?.PaletteFormat ?? -1;

        public Func<SaveTabEventArgs, Task<bool>> SaveFilesDelegate { get; set; }
        public Action<IStateInfo> UpdateTabDelegate { get; set; }

        public ImageForm(IStateInfo stateInfo)
        {
            InitializeComponent();

            // ReSharper disable once SuspiciousTypeConversion.Global
            if (!(stateInfo.State is IImageState imageState))
                throw new InvalidOperationException($"This state is not an {nameof(IImageState)}.");

            ContractAssertions.IsNotNull(stateInfo, nameof(stateInfo));
            ContractAssertions.IsNotNull(imageState.Images, nameof(imageState.Images));

            if (imageState.SupportedEncodings == null && imageState.SupportedIndexEncodings == null)
                throw new InvalidOperationException("The plugin has no supported encodings defined.");
            if (imageState.SupportedIndexEncodings != null && imageState.SupportedPaletteEncodings == null)
                throw new InvalidOperationException("The plugin has no supported palette encodings defined.");

            // Check for ambiguous format values
            if (imageState.SupportedEncodings?.Keys.Any(x => imageState.SupportedIndexEncodings?.Keys.Contains(x) ?? false) ?? false)
                throw new InvalidOperationException($"Ambiguous image format identifiers in plugin {_imageState.GetType().FullName}.");

            // TODO: Check that all image infos contain supported image formats

            _stateInfo = stateInfo;
            _imageState = imageState;

            _images = imageState.Images?.Select(CreateImage).ToArray() ?? Array.Empty<Bitmap>();
            _imagePalettes = imageState.Images?.Select(CreatePalette).ToArray() ?? Array.Empty<IList<Color>>();
            _bestImages = _images.Select(x => (Bitmap)x.Clone()).ToArray();

            imbPreview.Image = _images.FirstOrDefault();

            // Populate format dropdown
            PopulateFormatDropdown();

            // Populate palette format dropdown
            PopulatePaletteDropdown();

            // Populate border style drop down
            PopulateBorderStyleDropdown();

            // Update form elements
            UpdateForm();
            UpdatePreview();
            UpdateImageList();
        }

        /// <summary>
        /// Creates an image out of an <see cref="ImageInfo"/>.
        /// </summary>
        /// <param name="imageInfo">The <see cref="ImageInfo"/> to create the image from.</param>
        /// <returns>The created image.</returns>
        private Bitmap CreateImage(ImageInfo imageInfo)
        {
            if (imageInfo is IndexImageInfo indexInfo)
            {
                var indexTranscoder = indexInfo.Configuration
                    .TranscodeWith(imageSize => _imageState.SupportedIndexEncodings[indexInfo.ImageFormat])
                    .TranscodePaletteWith(() => _imageState.SupportedPaletteEncodings[indexInfo.PaletteFormat])
                    .Build();

                return (Bitmap)indexTranscoder.Decode(indexInfo.ImageData, indexInfo.PaletteData, indexInfo.ImageSize);
            }

            var transcoder = imageInfo.Configuration
                .TranscodeWith(imageSize => _imageState.SupportedEncodings[imageInfo.ImageFormat])
                .Build();

            return (Bitmap)transcoder.Decode(imageInfo.ImageData, imageInfo.ImageSize);
        }

        /// <summary>
        /// Creates a list of colors of an <see cref="ImageInfo"/>.
        /// </summary>
        /// <param name="imageInfo">The <see cref="ImageInfo"/> to create the palette from.</param>
        /// <returns>The created palette.</returns>
        private IList<Color> CreatePalette(ImageInfo imageInfo)
        {
            if (!(imageInfo is IndexImageInfo indexInfo))
                return null;

            return _imageState.SupportedPaletteEncodings[indexInfo.PaletteFormat].Load(indexInfo.PaletteData, Environment.ProcessorCount).ToArray();
        }

        /// <summary>
        /// Transcodes the image at <paramref name="imageIndex"/> into the given formats.
        /// </summary>
        /// <param name="imageIndex">The index to the image to transcode.</param>
        /// <param name="newImageFormat">The new image format.</param>
        /// <param name="newPaletteFormat">The new palette format. -1, if not an index format.</param>
        /// <param name="replaceImageInfo">If the image info at <paramref name="imageIndex"/> should be replaced by the new image information.</param>
        /// <returns>The transcoded image.</returns>
        private Bitmap Transcode(int imageIndex, int newImageFormat, int newPaletteFormat, bool replaceImageInfo)
        {
            var image = _bestImages[imageIndex] ?? _images[imageIndex];

            if (_imageState.Images[imageIndex].ImageFormat == newImageFormat && newPaletteFormat == -1)
                return image;

            Bitmap newImage;
            if (_imageState.SupportedEncodings.ContainsKey(newImageFormat))
            {
                // Transcode image to new image format
                var transcoder = _imageState.Images[imageIndex].Configuration
                    .WithTaskCount(1)
                    .TranscodeWith(imageSize => _imageState.SupportedEncodings[newImageFormat])
                    .Build();

                var imageData = transcoder.Encode(image);
                newImage = (Bitmap)transcoder.Decode(imageData, _imageState.Images[imageIndex].ImageSize);

                if (replaceImageInfo)
                {
                    // If old image was an indexed image info
                    if (_imageState.Images[imageIndex] is IndexImageInfo indexInfo)
                    {
                        // Convert it to a normal image info
                        _imageState.Images[imageIndex] = _imageState.ConvertToImageInfo(indexInfo);
                    }

                    // And set its image properties
                    _imageState.Images[imageIndex].ImageFormat = newImageFormat;
                    _imageState.Images[imageIndex].ImageData = imageData;
                    _imageState.Images[imageIndex].ImageSize = image.Size;
                }
            }
            else
            {
                if (newPaletteFormat == -1)
                    throw new InvalidOperationException("A palette format has to be set.");

                // Transcode image to new image format
                var transcoder = _imageState.Images[imageIndex].Configuration
                    .TranscodeWith(imageSize => _imageState.SupportedIndexEncodings[newImageFormat])
                    .TranscodePaletteWith(() => _imageState.SupportedPaletteEncodings[newPaletteFormat])
                    .Build();

                var (indexData, paletteData) = transcoder.Encode(image);
                newImage = (Bitmap)transcoder.Decode(indexData, paletteData, _imageState.Images[imageIndex].ImageSize);

                if (replaceImageInfo)
                {
                    // If old image was not an indexed image info
                    var indexInfo = _imageState.Images[imageIndex] as IndexImageInfo;
                    if (indexInfo == null)
                    {
                        // Convert it to an indexed image info
                        indexInfo = _imageState.ConvertToIndexImageInfo(
                            _imageState.Images[imageIndex], newPaletteFormat, paletteData);
                        _imageState.Images[imageIndex] = indexInfo;
                    }

                    // And set its image properties
                    indexInfo.ImageFormat = newImageFormat;
                    indexInfo.ImageData = indexData;
                    indexInfo.ImageSize = image.Size;
                    indexInfo.PaletteFormat = newPaletteFormat;
                    indexInfo.PaletteData = paletteData;
                }
            }

            return newImage;
        }

        #region Dropdown population

        private void PopulateFormatDropdown()
        {
            var items = new List<ToolStripItem>();

            if (_imageState.SupportedEncodings != null)
            {
                items.AddRange(_imageState.SupportedEncodings.Select(f => new ToolStripMenuItem
                {
                    Text = f.Value.FormatName,
                    Tag = f.Key,
                    Checked = f.Key == _imageState.Images[_selectedImageIndex].ImageFormat
                }));
            }

            if (_imageState.SupportedIndexEncodings != null)
            {
                items.AddRange(_imageState.SupportedIndexEncodings.Select(f => new ToolStripMenuItem
                {
                    Text = f.Value.FormatName,
                    Tag = f.Key,
                    Checked = f.Key == _imageState.Images[_selectedImageIndex].ImageFormat
                }));
            }

            tsbFormat.DropDownItems.AddRange(items.ToArray());
            if (tsbFormat.DropDownItems.Count > 0)
                foreach (var tsb in tsbFormat.DropDownItems)
                    ((ToolStripMenuItem)tsb).Click += tsbFormat_Click;
        }

        private void PopulatePaletteDropdown()
        {
            var items = new List<ToolStripItem>();

            if (_imageState.SupportedPaletteEncodings != null)
            {
                items.AddRange(_imageState.SupportedPaletteEncodings.Select(f => new ToolStripMenuItem
                {
                    Text = f.Value.FormatName,
                    Tag = f.Key,
                    Checked = f.Key == _imageState.Images[_selectedImageIndex].ImageFormat
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
            _images[_selectedImageIndex] = Transcode(_selectedImageIndex, newImageFormat, SelectedPaletteFormat, true);

            if (SelectedImageInfo is IndexImageInfo)
                _imagePalettes[_selectedImageIndex] = CreatePalette(SelectedImageInfo);
            else
                _imagePalettes[_selectedImageIndex] = null;

            UpdateForm();
            UpdateImageList();
            UpdatePreview();
        }

        private void tsbPalette_Click(object sender, EventArgs e)
        {
            var tsb = (ToolStripMenuItem)sender;

            var newPaletteFormat = (int)tsb.Tag;
            _images[_selectedImageIndex] = Transcode(_selectedImageIndex, SelectedImageFormat, newPaletteFormat, true);

            if (SelectedImageInfo is IndexImageInfo)
                _imagePalettes[_selectedImageIndex] = CreatePalette(SelectedImageInfo);
            else
                _imagePalettes[_selectedImageIndex] = null;

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
            tslZoom.Text = "Zoom: " + imbPreview.Zoom + "%";
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
                tslTool.Text = "Tool: Pan";
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
                tslTool.Text = "Tool: Zoom";
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
            _selectedImageIndex = treBitmaps.SelectedNode.Index;
            UpdatePreview();
        }

        private void tsbSave_Click(object sender, EventArgs e)
        {
            Save(UPath.Empty);
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

        private void imbPreview_MouseClick(object sender, MouseEventArgs e)
        {
            if (_setIndexInImage && _paletteChosenColorIndex >= 0)
                SetIndexInImage(e.Location, _paletteChosenColorIndex);
            else
                SetColorInPalette(clrDialog.Color, GetPaletteIndexByImageLocation, e.Location);
        }

        private void tsbPaletteImport_Click(object sender, EventArgs e)
        {
            ImportPalette();
        }

        private void tsbPaletteExport_Click(object sender, EventArgs e)
        {
            ExportPalette();
        }

        private void pbPalette_MouseClick(object sender, MouseEventArgs e)
        {
            if (_paletteChooseColor)
                _paletteChosenColorIndex = GetPaletteIndex(e.Location);
            else
                SetColorInPalette(clrDialog.Color, GetPaletteIndex, e.Location);
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
            var sfd = new SaveFileDialog
            {
                FileName = _stateInfo.FilePath.GetName(),
                Filter = "All Files (*.*)|*.*"
            };

            if (sfd.ShowDialog() != DialogResult.OK)
            {
                MessageBox.Show("No save location selected", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Save(sfd.FileName);
        }

        private async void Save(UPath savePath)
        {
            if (savePath == UPath.Empty)
                savePath = _stateInfo.FilePath;

            var result = await SaveFilesDelegate(new SaveTabEventArgs(_stateInfo, savePath));

            if (!result)
                MessageBox.Show("The file could not be saved.", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            UpdateForm();
        }

        private void ExportPng()
        {
            var sfd = new SaveFileDialog
            {
                Title = "Export Png...",
                InitialDirectory = Settings.Default.LastDirectory,
                FileName = _stateInfo.FilePath.GetNameWithoutExtension() + "." + _selectedImageIndex.ToString("00") + ".png",
                Filter = "Portable Network Graphics (*.png)|*.png",
                AddExtension = true
            };

            if (sfd.ShowDialog() == DialogResult.OK)
                _images[_selectedImageIndex].Save(sfd.FileName, ImageFormat.Png);
        }

        #endregion

        #region Import

        private void ImportPng()
        {
            var ofd = new OpenFileDialog
            {
                Title = "Import Png...",
                InitialDirectory = Settings.Default.LastDirectory,
                Filter = "Portable Network Graphics (*.png)|*.png"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
                Import(ofd.FileName);
        }

        private void Import(UPath filePath)
        {
            try
            {
                _bestImages[_selectedImageIndex] = new Bitmap(filePath.FullName);
                _images[_selectedImageIndex] = Transcode(_selectedImageIndex, SelectedImageFormat, SelectedPaletteFormat, true);

                if (SelectedImageInfo is IndexImageInfo)
                    _imagePalettes[_selectedImageIndex] = CreatePalette(SelectedImageInfo);
                else
                    _imagePalettes[_selectedImageIndex] = null;

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

        public void UpdateForm()
        {
            tsbSave.Enabled = _imageState is ISaveFiles;
            tsbSaveAs.Enabled = _imageState is ISaveFiles && _stateInfo.ParentStateInfo == null;

            var isIndexed = SelectedImageInfo is IndexImageInfo;
            tslPalette.Visible = isIndexed;
            tsbPalette.Visible = isIndexed;
            tsbPalette.Enabled = isIndexed && (_imageState.SupportedPaletteEncodings?.Any() ?? false);
            pbPalette.Enabled = isIndexed;
            tsbPaletteImport.Enabled = isIndexed;

            splProperties.Panel2Collapsed = !isIndexed;

            tsbFormat.Enabled = _imageState.SupportedEncodings?.Any() ?? false;

            UpdateTabDelegate?.Invoke(_stateInfo);
        }

        private void UpdatePreview()
        {
            if (_imageState.Images.Count > 0)
            {
                imbPreview.Image = _images[_selectedImageIndex];
                imbPreview.Zoom -= 1;
                imbPreview.Zoom += 1;
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
            tsbFormat.Text = _imageState.SupportedEncodings[SelectedImageFormat].FormatName;
            tsbFormat.Tag = SelectedImageInfo.ImageFormat;

            // Update selected format
            foreach (ToolStripMenuItem tsm in tsbFormat.DropDownItems)
                tsm.Checked = (int)tsm.Tag == SelectedImageInfo.ImageFormat;

            if (SelectedImageInfo is IndexImageInfo indexedInfo)
            {
                // PaletteData Dropdown
                tsbPalette.Text = _imageState.SupportedPaletteEncodings[indexedInfo.PaletteFormat].FormatName;
                tsbPalette.Tag = indexedInfo.PaletteFormat;

                // Update selected palette format
                foreach (ToolStripMenuItem tsm in tsbPalette.DropDownItems)
                    tsm.Checked = (int)tsm.Tag == indexedInfo.PaletteFormat;

                // PaletteData Picture Box
                var dimPalette = (int)Math.Ceiling(Math.Sqrt(indexedInfo.ColorCount));
                var paletteImg = _imagePalettes[_selectedImageIndex]
                    .ToBitmap(new Size(dimPalette, dimPalette));
                if (paletteImg != null)
                    pbPalette.Image = paletteImg;
            }

            // Dimensions
            tslWidth.Text = SelectedImageInfo.ImageSize.Width.ToString();
            tslHeight.Text = SelectedImageInfo.ImageSize.Height.ToString();
        }

        private void UpdateImageList()
        {
            if (_imageState.Images.Count <= 0)
                return;

            treBitmaps.BeginUpdate();
            treBitmaps.Nodes.Clear();
            imlBitmaps.Images.Clear();
            imlBitmaps.TransparentColor = Color.Transparent;
            imlBitmaps.ImageSize = new Size(Settings.Default.ThumbnailWidth, Settings.Default.ThumbnailHeight);
            treBitmaps.ItemHeight = Settings.Default.ThumbnailHeight + 6;

            for (var i = 0; i < _imageState.Images.Count; i++)
            {
                imlBitmaps.Images.Add(i.ToString(), GenerateThumbnail(_images[i]));
                treBitmaps.Nodes.Add(new TreeNode
                {
                    Text = !string.IsNullOrEmpty(_imageState.Images[i].Name) ? _imageState.Images[i].Name : i.ToString("00"),
                    Tag = i,
                    ImageKey = i.ToString(),
                    SelectedImageKey = i.ToString()
                });
            }

            treBitmaps.EndUpdate();
        }

        #endregion

        #region Private methods

        private void SetGridColor(Color color, Action<Color> setColorToProperties)
        {
            clrDialog.Color = imbPreview.GridColor;
            if (clrDialog.ShowDialog() != DialogResult.OK)
                return;

            setColorToProperties(color);

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

        private void SetColorInPalette(Color setColor, Func<Point, int> indexFunc, Point controlPoint)
        {
            if (!(SelectedImageInfo is IndexImageInfo indexInfo))
                return;

            DisablePaletteControls();
            DisableImageControls();

            var index = indexFunc(controlPoint);
            if (index < 0 || index >= indexInfo.ColorCount)
            {
                UpdateForm();
                return;
            }

            if (clrDialog.ShowDialog() != DialogResult.OK)
            {
                UpdateForm();
                return;
            }

            IList<int> indices;
            try
            {
                indices = _images[_selectedImageIndex].ToIndices(_imagePalettes[_selectedImageIndex]).ToList();

                _imagePalettes[_selectedImageIndex][index] = setColor;

                indexInfo.PaletteData = _imageState.SupportedPaletteEncodings[indexInfo.PaletteFormat]
                    .Save(_imagePalettes[_selectedImageIndex], Environment.ProcessorCount);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Exception catched", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateForm();
                return;
            }

            // TODO: Currently reset the best bitmap to quantized image, so Encode will encode the quantized image with changed palette
            _bestImages[_selectedImageIndex] = _images[_selectedImageIndex] =
                indices.ToBitmap(_imagePalettes[_selectedImageIndex], SelectedImageInfo.ImageSize);

            UpdateForm();
            UpdatePreview();
            UpdateImageList();
        }

        private void SetIndexInImage(Point controlPoint, int newIndex)
        {
            if (!(SelectedImageInfo is IndexImageInfo indexInfo))
                return;

            if (newIndex >= indexInfo.ColorCount)
                return;

            DisablePaletteControls();
            DisableImageControls();

            var pointInImg = GetPointInImage(controlPoint);
            if (pointInImg == Point.Empty)
            {
                UpdateForm();
                return;
            }

            IList<int> indices;
            try
            {
                indices = _images[_selectedImageIndex].ToIndices(_imagePalettes[_selectedImageIndex]).ToList();

                indices[pointInImg.Y * SelectedImageInfo.ImageSize.Width + pointInImg.X] = newIndex;

                indexInfo.ImageData = _imageState.SupportedIndexEncodings[indexInfo.PaletteFormat].Save(indices, _imagePalettes[_selectedImageIndex], Environment.ProcessorCount);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Exception catched", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateForm();
                return;
            }

            // TODO: Currently reset the best bitmap to quantized image, so Encode will encode the quantized image with changed palette
            _bestImages[_selectedImageIndex] = _images[_selectedImageIndex] =
                indices.ToBitmap(_imagePalettes[_selectedImageIndex], SelectedImageInfo.ImageSize);

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
            if (!imbPreview.IsPointInImage(controlPoint))
                return Point.Empty;

            return imbPreview.PointToImage(controlPoint);
        }

        private int GetPaletteIndexByImageLocation(Point point)
        {
            var pointInImg = GetPointInImage(point);
            if (pointInImg == Point.Empty)
                return -1;

            var pixelColor = _images[_selectedImageIndex].GetPixel(pointInImg.X, pointInImg.Y);

            return _imagePalettes[_selectedImageIndex].IndexOf(pixelColor);
        }

        private void ImportPalette()
        {
            if (!(SelectedImageInfo is IndexImageInfo indexInfo))
                return;

            var colors = LoadPaletteFile();
            if (colors == null)
                return;

            DisablePaletteControls();
            DisableImageControls();

            IList<int> indices;
            try
            {
                var paletteEncoding = _imageState.SupportedPaletteEncodings[indexInfo.PaletteFormat];
                indices = _images[_selectedImageIndex].ToIndices(_imagePalettes[_selectedImageIndex]).ToList();

                _imagePalettes[_selectedImageIndex] = colors;
                indexInfo.PaletteData = paletteEncoding.Save(colors, Environment.ProcessorCount);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Exception catched", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateForm();
                return;
            }

            // TODO: Currently reset the best bitmap to quantized image, so Encode will encode the quantized image with changed palette
            _bestImages[_selectedImageIndex] = _images[_selectedImageIndex] =
                indices.ToBitmap(colors, SelectedImageInfo.ImageSize);

            UpdateForm();
            UpdatePreview();
            UpdateImageList();
        }

        private IList<Color> LoadPaletteFile()
        {
            var ofd = new OpenFileDialog
            {
                Title = "Open palette...",
                InitialDirectory = Settings.Default.LastDirectory,
                Filter = "Kuriimu PaletteData (*.kpal)|*.kpal|Microsoft RIFF PaletteData (*.pal)|*.pal"
            };

            if (ofd.ShowDialog() != DialogResult.OK)
            {
                MessageBox.Show("Couldn't open palette file.", "Invalid file", MessageBoxButtons.OK,
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
                    MessageBox.Show(e.ToString(), "Exception catched.", MessageBoxButtons.OK,
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
                    MessageBox.Show(e.ToString(), "Exception catched.", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }

            return palette;
        }

        private void ExportPalette()
        {
            if (!(SelectedImageInfo is IndexImageInfo))
                return;

            SavePaletteFile(_imagePalettes[_selectedImageIndex]);
        }

        private void SavePaletteFile(IList<Color> colors)
        {
            var sfd = new SaveFileDialog
            {
                Title = "Save palette...",
                InitialDirectory = Settings.Default.LastDirectory,
                Filter = "Kuriimu PaletteData (*.kpal)|*.kpal"
            };

            if (sfd.ShowDialog() != DialogResult.OK)
            {
                MessageBox.Show("Couldn't save palette file.", "Invalid file", MessageBoxButtons.OK,
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
                MessageBox.Show(e.ToString(), "Exception catched.", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        #endregion
    }
}
