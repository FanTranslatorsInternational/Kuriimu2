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
using Kontract.Models.Images;
using Kontract.Models.IO;
using Kore.Utilities.Palettes;
using Kuriimu2.WinForms.Interfaces;
using Kuriimu2.WinForms.Properties;

namespace Kuriimu2.WinForms.FormatForms
{
    // TODO: Recode image to data only at saving
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
        private readonly IProgressContext _progressContext;

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

        public ImageForm(IStateInfo stateInfo, IProgressContext progressContext)
        {
            InitializeComponent();

            // ReSharper disable once SuspiciousTypeConversion.Global
            if (!(stateInfo.State is IImageState imageState))
                throw new InvalidOperationException($"This state is not an {nameof(IImageState)}.");

            ContractAssertions.IsNotNull(stateInfo, nameof(stateInfo));
            ContractAssertions.IsNotNull(imageState.Images, nameof(imageState.Images));

            // Check integrity of the image state
            CheckIntegrity(imageState);

            _stateInfo = stateInfo;
            _imageState = imageState;
            _progressContext = progressContext;

            _imagePalettes = CreatePalettes(imageState.Images).ToArray();
            _images = CreateImages(imageState.Images).ToArray();
            progressContext.ReportProgress("Done", 1, 1);
            _bestImages = _images.Select(x => (Bitmap)x.Clone()).ToArray();

            imbPreview.Image = _images.FirstOrDefault();

            // Populate format dropdown
            PopulateFormatDropdown();

            // Populate palette format dropdown
            PopulatePaletteDropdown(SelectedImageInfo);

            // Populate border style drop down
            PopulateBorderStyleDropdown();

            // Update form elements
            UpdateForm();
            UpdatePreview();
            UpdateImageList();
        }

        private void CheckIntegrity(IImageState imageState)
        {
            if (imageState.SupportedEncodings == null && imageState.SupportedIndexEncodings == null)
                throw new InvalidOperationException("The plugin has no supported encodings defined.");
            if (imageState.SupportedIndexEncodings != null && imageState.SupportedPaletteEncodings == null)
                throw new InvalidOperationException("The plugin has no supported palette encodings defined.");

            // Check for ambiguous format values
            if (imageState.SupportedEncodings?.Keys.Any(x => imageState.SupportedIndexEncodings?.Keys.Contains(x) ?? false) ?? false)
                throw new InvalidOperationException($"Ambiguous image format identifiers in plugin {_imageState.GetType().FullName}.");

            // Check that all image infos contain supported image formats
            foreach (var image in imageState.Images)
            {
                var encodingSupported = imageState.SupportedEncodings?.ContainsKey(image.ImageFormat) ?? false;
                var indexEncodingSupported = imageState.SupportedIndexEncodings?.ContainsKey(image.ImageFormat) ?? false;
                if (!encodingSupported && !indexEncodingSupported)
                    throw new InvalidOperationException($"Image format {image.ImageFormat} is not supported by the plugin.");

                if (indexEncodingSupported && !(image is IndexImageInfo))
                    throw new InvalidOperationException($"The image format {image.ImageFormat} is indexed, but the image is not.");
                if (encodingSupported && image is IndexImageInfo)
                    throw new InvalidOperationException($"The image format {image.ImageFormat} is not indexed, but the image is.");

                if (image is IndexImageInfo indexImage)
                {
                    var indexEncoding = imageState.SupportedIndexEncodings[indexImage.ImageFormat];
                    if (indexEncoding.PaletteEncodingIndices != null &&
                        !indexEncoding.PaletteEncodingIndices.All(x => imageState.SupportedPaletteEncodings.ContainsKey(x)))
                        throw new InvalidOperationException($"The image format {image.ImageFormat} depends on palette encodings not supported by the plugin.");
                }
            }
        }

        /// <summary>
        /// Creates images out of <see cref="ImageInfo"/>s.
        /// </summary>
        /// <param name="infos">The <see cref="ImageInfo"/>s to create the images from.</param>
        /// <returns>The created images.</returns>
        private IEnumerable<Bitmap> CreateImages(IList<ImageInfo> infos)
        {
            if (!infos?.Any() ?? true)
                yield break;

            var progressPart = _progressContext.MaxPercentage / infos.Count;
            for (var i = 0; i < infos.Count; i++)
            {
                var progress = _progressContext.CreateScope($"Image {i + 1}: ", i * progressPart,
                    Math.Min(_progressContext.MaxPercentage, (i + 1) * progressPart));

                if (infos[i] is IndexImageInfo indexInfo)
                {
                    var indexTranscoder = indexInfo.Configuration.Clone()
                        .TranscodeWith(imageSize => _imageState.SupportedIndexEncodings[indexInfo.ImageFormat].Encoding)
                        .TranscodePaletteWith(() => _imageState.SupportedPaletteEncodings[indexInfo.PaletteFormat])
                        .Build();

                    yield return (Bitmap)indexTranscoder.Decode(indexInfo.ImageData, indexInfo.PaletteData, indexInfo.ImageSize, progress);
                }
                else
                {
                    var transcoder = infos[i].Configuration.Clone()
                        .TranscodeWith(imageSize => _imageState.SupportedEncodings[infos[i].ImageFormat])
                        .Build();

                    yield return (Bitmap)transcoder.Decode(infos[i].ImageData, infos[i].ImageSize, progress);
                }
            }
        }

        /// <summary>
        /// Creates a list of colors of the <see cref="ImageInfo"/>s.
        /// </summary>
        /// <param name="infos">The <see cref="ImageInfo"/>s to create the palettes from.</param>
        /// <returns>The created palettes.</returns>
        private IEnumerable<IList<Color>> CreatePalettes(IList<ImageInfo> infos)
        {
            if (!infos?.Any() ?? true)
                yield break;

            var progressPart = _progressContext.MaxPercentage / infos.Count;
            for (var i = 0; i < infos.Count; i++)
            {
                var progress = _progressContext.CreateScope($"Palette {i + 1}: ", i * progressPart,
                    Math.Min(_progressContext.MaxPercentage, (i + 1) * progressPart));

                yield return CreatePalette(infos[i], progress);
            }
        }

        private IList<Color> CreatePalette(ImageInfo imageInfo, IProgressContext progress = null)
        {
            if (!(imageInfo is IndexImageInfo indexInfo))
            {
                return null;
            }

            progress?.ReportProgress("Decode colors", 0, 1);
            var palette = _imageState.SupportedPaletteEncodings[indexInfo.PaletteFormat].Load(indexInfo.PaletteData, Environment.ProcessorCount).ToArray();
            progress?.ReportProgress("Done", 1, 1);

            return palette;
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
            var imageInfo = _imageState.Images[imageIndex];
            var indexInfo = imageInfo as IndexImageInfo;

            if (imageInfo.ImageFormat == newImageFormat &&
                newPaletteFormat == -1)
                return image;

            if (indexInfo != null &&
                indexInfo.PaletteFormat == newPaletteFormat &&
                imageInfo.ImageFormat == newImageFormat)
                return image;

            var encodeProgress = _progressContext.CreateScope(0, 50);
            var decodeProgress = _progressContext.CreateScope(50, 100);

            Bitmap newImage;
            if (_imageState.SupportedEncodings.ContainsKey(newImageFormat))
            {
                // Transcode image to new image format
                var transcoder = imageInfo.Configuration.Clone()
                    .TranscodeWith(imageSize => _imageState.SupportedEncodings[newImageFormat])
                    .Build();

                var imageData = transcoder.Encode(image, encodeProgress);
                newImage = (Bitmap)transcoder.Decode(imageData, imageInfo.ImageSize, decodeProgress);

                if (replaceImageInfo)
                {
                    // If old image was an indexed image info
                    if (indexInfo != null)
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
                var transcoder = imageInfo.Configuration.Clone()
                    .TranscodeWith(imageSize => _imageState.SupportedIndexEncodings[newImageFormat].Encoding)
                    .TranscodePaletteWith(() => _imageState.SupportedPaletteEncodings[newPaletteFormat])
                    .Build();

                var (indexData, paletteData) = transcoder.Encode(image, encodeProgress);
                newImage = (Bitmap)transcoder.Decode(indexData, paletteData, imageInfo.ImageSize, decodeProgress);

                if (replaceImageInfo)
                {
                    // If old image was not an indexed image info
                    if (indexInfo == null)
                    {
                        // Convert it to an indexed image info
                        _imageState.Images[imageIndex] = indexInfo = _imageState.ConvertToIndexImageInfo(
                            imageInfo, newPaletteFormat, paletteData);
                    }

                    // And set its image properties
                    indexInfo.ImageFormat = newImageFormat;
                    indexInfo.ImageData = indexData;
                    indexInfo.ImageSize = image.Size;
                    indexInfo.PaletteFormat = newPaletteFormat;
                    indexInfo.PaletteData = paletteData;
                }
            }

            _progressContext.ReportProgress("Done", 1, 1);

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
                    Text = f.Value.Encoding.FormatName,
                    Tag = f.Key,
                    Checked = f.Key == _imageState.Images[_selectedImageIndex].ImageFormat
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
            if (_imageState.SupportedPaletteEncodings != null)
            {
                var paletteEncodings = _imageState.SupportedPaletteEncodings;

                var indices = _imageState.SupportedIndexEncodings[imageInfo.ImageFormat].PaletteEncodingIndices;
                if (imageInfo.IsIndexed && indices != null)
                    paletteEncodings = _imageState.SupportedPaletteEncodings.Where(x => indices.Contains(x.Key))
                        .ToDictionary(x => x.Key, y => y.Value);

                items.AddRange(paletteEncodings.Select(f => new ToolStripMenuItem
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
            var paletteFormat = SelectedPaletteFormat;
            if (_imageState.SupportedIndexEncodings.ContainsKey(newImageFormat) && SelectedPaletteFormat == -1)
                paletteFormat = _imageState.SupportedIndexEncodings[newImageFormat].PaletteEncodingIndices?.First() ??
                                _imageState.SupportedPaletteEncodings.First().Key;

            DisableImageControls();
            DisablePaletteControls();

            _images[_selectedImageIndex] = Transcode(_selectedImageIndex, newImageFormat, paletteFormat, true);

            if (SelectedImageInfo is IndexImageInfo)
                _imagePalettes[_selectedImageIndex] = CreatePalette(SelectedImageInfo, _progressContext);
            else
                _imagePalettes[_selectedImageIndex] = null;

            PopulatePaletteDropdown(SelectedImageInfo);

            UpdateForm();
            UpdateImageList();
            UpdatePreview();
        }

        private void tsbPalette_Click(object sender, EventArgs e)
        {
            var tsb = (ToolStripMenuItem)sender;

            DisableImageControls();
            DisablePaletteControls();

            var newPaletteFormat = (int)tsb.Tag;
            _images[_selectedImageIndex] = Transcode(_selectedImageIndex, SelectedImageFormat, newPaletteFormat, true);

            if (SelectedImageInfo is IndexImageInfo)
                _imagePalettes[_selectedImageIndex] = CreatePalette(SelectedImageInfo, _progressContext);
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
            if (_selectedImageIndex != treBitmaps.SelectedNode.Index)
            {
                _selectedImageIndex = treBitmaps.SelectedNode.Index;
                UpdatePreview();
            }
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
                    _imagePalettes[_selectedImageIndex] = CreatePalette(SelectedImageInfo, _progressContext);
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

            var isIndexed = SelectedImageInfo.IsIndexed;
            tslPalette.Visible = isIndexed;
            tsbPalette.Visible = isIndexed;
            tsbPalette.Enabled = isIndexed && (_imageState.SupportedPaletteEncodings?.Any() ?? false);
            pbPalette.Enabled = isIndexed;
            tsbPaletteImport.Enabled = isIndexed;

            splProperties.Panel2Collapsed = !isIndexed;

            tsbFormat.Enabled = _imageState.SupportedEncodings?.Any() ?? false;

            imbPreview.Enabled = _imageState.Images.Any();

            UpdateTabDelegate?.Invoke(_stateInfo);
        }

        private void UpdatePreview()
        {
            if (_imageState.Images.Count > 0)
            {
                var horizontalScroll = imbPreview.HorizontalScroll.Value;
                var verticalScroll = imbPreview.VerticalScroll.Value;

                imbPreview.Image = _images[_selectedImageIndex];
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
            tsbFormat.Text = _imageState.SupportedEncodings.ContainsKey(SelectedImageFormat) ?
                _imageState.SupportedEncodings[SelectedImageFormat].FormatName :
                _imageState.SupportedIndexEncodings[SelectedImageFormat].Encoding.FormatName;
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
            if (SelectedImageInfo is IndexImageInfo indexedInfo)
            {
                // PaletteData Dropdown
                tsbPalette.Text = _imageState.SupportedPaletteEncodings[indexedInfo.PaletteFormat].FormatName;
                tsbPalette.Tag = indexedInfo.PaletteFormat;

                // Update selected palette format
                foreach (ToolStripMenuItem tsm in tsbPalette.DropDownItems)
                    tsm.Checked = (int)tsm.Tag == indexedInfo.PaletteFormat;

                // PaletteData Picture Box
                var dimPalette = (int)Math.Ceiling(Math.Sqrt(_imagePalettes[_selectedImageIndex].Count));
                var paletteImg = _imagePalettes[_selectedImageIndex]
                    .ToBitmap(new Size(dimPalette, dimPalette));
                if (paletteImg != null)
                    pbPalette.Image = paletteImg;
            }
        }

        private void UpdateImageList()
        {
            if (_imageState.Images.Count <= 0)
                return;

            var selectedIndex = treBitmaps.SelectedNode?.Index ?? -1;

            treBitmaps.BeginUpdate();
            treBitmaps.Nodes.Clear();
            imlBitmaps.Images.Clear();
            imlBitmaps.TransparentColor = Color.Transparent;
            imlBitmaps.ImageSize = new Size(Settings.Default.ThumbnailWidth, Settings.Default.ThumbnailHeight);
            treBitmaps.ItemHeight = Settings.Default.ThumbnailHeight + 6;

            for (var i = 0; i < _imageState.Images.Count; i++)
            {
                imlBitmaps.Images.Add(i.ToString(), GenerateThumbnail(_images[i]));
                var treeNode = new TreeNode
                {
                    Text = !string.IsNullOrEmpty(_imageState.Images[i].Name)
                        ? _imageState.Images[i].Name
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
            if (!(SelectedImageInfo is IndexImageInfo indexInfo))
                return;

            DisablePaletteControls();
            DisableImageControls();

            var index = indexFunc(controlPoint);
            if (index < 0 || index >= _imagePalettes[_selectedImageIndex].Count)
            {
                UpdateForm();
                return;
            }

            if (clrDialog.ShowDialog() != DialogResult.OK)
            {
                UpdateForm();
                return;
            }

            IList<int> indices = Array.Empty<int>();
            try
            {
                await Task.Run(() =>
                {
                    var progresses = _progressContext.SplitIntoScopes(2);

                    var setMaxProgress = progresses[0].SetMaxValue(_images[_selectedImageIndex].Width * _images[_selectedImageIndex].Height);
                    indices = _images[_selectedImageIndex]
                        .ToIndices(_imagePalettes[_selectedImageIndex])
                        .AttachProgress(setMaxProgress, "Decode indices")
                        .ToList();

                    _imagePalettes[_selectedImageIndex][index] = clrDialog.Color;

                    setMaxProgress = progresses[1].SetMaxValue(_imagePalettes[_selectedImageIndex].Count);
                    indexInfo.PaletteData = _imageState.SupportedPaletteEncodings[indexInfo.PaletteFormat]
                        .Save(_imagePalettes[_selectedImageIndex].AttachProgress(setMaxProgress, "Encode palette colors"), Environment.ProcessorCount);

                    _progressContext.ReportProgress("Done", 1, 1);
                });
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

        private async Task SetIndexInImage(Point controlPoint, int newIndex)
        {
            if (!(SelectedImageInfo is IndexImageInfo indexInfo))
                return;

            if (newIndex >= _imagePalettes[_selectedImageIndex].Count)
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
                    var progresses = _progressContext.SplitIntoScopes(2);

                    var setMaxProgress = progresses[0].SetMaxValue(_images[_selectedImageIndex].Width * _images[_selectedImageIndex].Height);
                    var indices = _images[_selectedImageIndex]
                        .ToIndices(_imagePalettes[_selectedImageIndex])
                        .AttachProgress(setMaxProgress, "Decode indices")
                        .ToList();

                    indices[pointInImg.Y * SelectedImageInfo.ImageSize.Width + pointInImg.X] = newIndex;

                    setMaxProgress = progresses[1].SetMaxValue(_images[_selectedImageIndex].Width * _images[_selectedImageIndex].Height);
                    indexInfo.ImageData = _imageState.SupportedIndexEncodings[indexInfo.ImageFormat].Encoding
                        .Save(indices.AttachProgress(setMaxProgress, "Encode indices"), _imagePalettes[_selectedImageIndex], Environment.ProcessorCount);

                    _progressContext.ReportProgress("Done", 1, 1);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Exception catched", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateForm();
                return;
            }

            // TODO: Currently reset the best bitmap to quantized image, so Encode will encode the quantized image with changed palette
            _bestImages[_selectedImageIndex].SetPixel(pointInImg.X, pointInImg.Y, _imagePalettes[_selectedImageIndex][newIndex]);
            _images[_selectedImageIndex].SetPixel(pointInImg.X, pointInImg.Y, _imagePalettes[_selectedImageIndex][newIndex]);

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
            if (!imbPreview.IsPointInImage(controlPoint))
                return Point.Empty;

            return imbPreview.PointToImage(controlPoint);
        }

        private int GetPaletteIndexByImageLocation(Point point)
        {
            var pointInImg = GetPointInImage(point);
            if (pointInImg == Point.Empty)
                return -1;

            var pixelColor = _images[_selectedImageIndex].GetPixel(pointInImg.X, pointInImg.Y).ToArgb();

            var palette = _imagePalettes[_selectedImageIndex];
            for (var i = 0; i < palette.Count; i++)
                if (palette[i].ToArgb() == pixelColor)
                    return i;

            return -1;
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
                var progresses = _progressContext.SplitIntoScopes(2);

                var setMaxProgress = progresses[0].SetMaxValue(_images[_selectedImageIndex].Width * _images[_selectedImageIndex].Height);
                indices = _images[_selectedImageIndex]
                    .ToIndices(_imagePalettes[_selectedImageIndex])
                    .AttachProgress(setMaxProgress, "Decode indices")
                    .ToList();

                _imagePalettes[_selectedImageIndex] = colors;

                var paletteEncoding = _imageState.SupportedPaletteEncodings[indexInfo.PaletteFormat];
                setMaxProgress = progresses[1].SetMaxValue(colors.Count);
                indexInfo.PaletteData = paletteEncoding.Save(colors.AttachProgress(setMaxProgress, "Encode palette colors"), Environment.ProcessorCount);

                _progressContext.ReportProgress("Done", 1, 1);
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

        private void pbPalette_Paint(object sender, PaintEventArgs e)
        {
            if (_paletteChosenColorIndex >= 0 &&
                _paletteChosenColorIndex < _imagePalettes[_selectedImageIndex].Count)
            {
                var dimPalette = (int)Math.Ceiling(Math.Sqrt(_imagePalettes[_selectedImageIndex].Count));

                var colorOnIndex = _imagePalettes[_selectedImageIndex][_paletteChosenColorIndex];
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
