using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Cyotek.Windows.Forms;
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
        Dictionary<string, string> _stylesText = new Dictionary<string, string>
        {
            ["None"] = "None",
            ["FixedSingle"] = "Simple",
            ["FixedSingleDropShadow"] = "Drop Shadow",
            ["FixedSingleGlowShadow"] = "Glow Shadow"
        };

        Dictionary<string, string> _stylesImages = new Dictionary<string, string>
        {
            ["None"] = "menu_border_none",
            ["FixedSingle"] = "menu_border_simple",
            ["FixedSingleDropShadow"] = "menu_border_drop_shadow",
            ["FixedSingleGlowShadow"] = "menu_border_glow_shadow"
        };

        private readonly IStateInfo _stateInfo;
        private readonly IImageState _imageState;

        private ImageInfo _selectedImageInfo;
        private int _selectedImageIndex;

        private IList<Image> _bestImages;
        private Image _thumbnailBackground;

        public Func<SaveTabEventArgs, Task<bool>> SaveFilesDelegate { get; set; }
        public Action<IStateInfo> UpdateTabDelegate { get; set; }

        public ImageForm(IStateInfo stateInfo)
        {
            InitializeComponent();

            _stateInfo = stateInfo;
            _imageState = _stateInfo.State as IImageState;

            _selectedImageInfo = _imageState.Images?.FirstOrDefault();
            _selectedImageIndex = 0;

            _bestImages = _imageState.Images.Select(x => (Image)x.Image.Clone()).ToArray();
            imbPreview.Image = _imageState.Images?.FirstOrDefault()?.Image;

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

        #region Dropdown population

        private void PopulateFormatDropdown()
        {
            var items = new List<ToolStripItem>();

            if (_imageState.SupportedEncodings != null)
                items.AddRange(_imageState.SupportedEncodings.Select(f => new ToolStripMenuItem
                {
                    Text = f.Value.FormatName,
                    Tag = f.Key,
                    Checked = f.Key == _selectedImageInfo.ImageFormat
                }));

            if (_imageState.SupportedIndexEncodings != null)
                items.AddRange(_imageState.SupportedIndexEncodings.Select(f => new ToolStripMenuItem
                {
                    Text = f.Value.FormatName,
                    Tag = f.Key,
                    Checked = f.Key == _selectedImageInfo.ImageFormat
                }));

            tsbFormat.DropDownItems.AddRange(items.ToArray());
            if (tsbFormat.DropDownItems.Count > 0)
                foreach (var tsb in tsbFormat.DropDownItems)
                    ((ToolStripMenuItem)tsb).Click += tsbFormat_Click;
        }

        private void PopulatePaletteDropdown()
        {
            var items = new List<ToolStripItem>();

            if (_imageState.SupportedPaletteEncodings != null)
                items.AddRange(_imageState.SupportedPaletteEncodings.Select(f => new ToolStripMenuItem
                {
                    Text = f.Value.FormatName,
                    Tag = f.Key,
                    Checked = f.Key == _selectedImageInfo.ImageFormat
                }));

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
                }).ToArray();

            tsbImageBorderStyle.DropDownItems.AddRange(items);
            foreach (var tsb in tsbImageBorderStyle.DropDownItems)
                ((ToolStripMenuItem)tsb).Click += tsbImageBorderStyle_Click;
        }

        #endregion

        #region Events

        private async void tsbFormat_Click(object sender, EventArgs e)
        {
            var tsb = (ToolStripMenuItem)sender;

            var newImageFormat = (int)tsb.Tag;
            if (_selectedImageInfo.ImageFormat != newImageFormat)
            {
                var clonedImage = (Bitmap)_bestImages[_selectedImageInfo.ImageFormat].Clone();

                if (_selectedImageInfo is IndexedImageInfo indexImageInfo)
                {
                    var paletteFormat = indexImageInfo.PaletteFormat;

                    var transcoder = _selectedImageInfo.Configuration
                        .TranscodeWith(imageSize => _imageState.SupportedIndexEncodings[newImageFormat])
                        .TranscodePaletteWith(() => _imageState.SupportedPaletteEncodings[paletteFormat])
                        .Build();

                    _selectedImageInfo.Image = await Task.Run(() =>
                    {
                        var (imageData, paletteData) = transcoder.Encode(clonedImage);
                        return (Bitmap)transcoder.Decode(imageData, paletteData);
                    });
                }
                else
                {
                    var transcoder = _selectedImageInfo.Configuration.TranscodeWith(imageSize =>
                        _imageState.SupportedEncodings[newImageFormat]).Build();

                    _selectedImageInfo.Image = await Task.Run(() => (Bitmap)transcoder.Decode(transcoder.Encode(clonedImage)));
                }

                foreach (ToolStripMenuItem tsm in tsbFormat.DropDownItems)
                    tsm.Checked = false;
                tsb.Checked = true;

                _selectedImageInfo.ImageFormat = newImageFormat;
            }

            UpdateForm();
            UpdateImageList();
            UpdatePreview();
        }

        private async void tsbPalette_Click(object sender, EventArgs e)
        {
            var tsb = (ToolStripMenuItem)sender;

            var newPaletteFormat = (int)tsb.Tag;
            if (_selectedImageInfo is IndexedImageInfo indexImageInfo &&
                indexImageInfo.PaletteFormat != newPaletteFormat)
            {
                var clonedImage = (Bitmap)_bestImages[_selectedImageInfo.ImageFormat].Clone();

                var imageFormat = _selectedImageInfo.ImageFormat;

                var transcoder = _selectedImageInfo.Configuration
                    .TranscodeWith(imageSize => _imageState.SupportedIndexEncodings[imageFormat])
                    .TranscodePaletteWith(() => _imageState.SupportedPaletteEncodings[newPaletteFormat])
                    .Build();

                _selectedImageInfo.Image = await Task.Run(() =>
                {
                    var (imageData, paletteData) = transcoder.Encode(clonedImage);
                    return (Bitmap)transcoder.Decode(imageData, paletteData);
                });

                foreach (ToolStripMenuItem tsm in tsbPalette.DropDownItems)
                    tsm.Checked = false;
                tsb.Checked = true;

                indexImageInfo.PaletteFormat = newPaletteFormat;
            }

            UpdateForm();
            UpdateImageList();
            UpdatePreview();
        }

        private void tsbGridColor1_Click(object sender, EventArgs e)
        {
            SetGridColor(imbPreview.GridColor, (clr) =>
            {
                imbPreview.GridColor = clr;
                Settings.Default.GridColor1 = clr;
                Settings.Default.Save();
            });
        }

        private void tsbGridColor2_Click(object sender, EventArgs e)
        {
            SetGridColor(imbPreview.GridColorAlternate, (clr) =>
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
            if (clrDialog.ShowDialog() != DialogResult.OK) return;

            imbPreview.ImageBorderColor = clrDialog.Color;
            Settings.Default.ImageBorderColor = clrDialog.Color;
            Settings.Default.Save();
            UpdatePreview();
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
                _selectedImageInfo.Image.Save(sfd.FileName, ImageFormat.Png);
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

        private async void Import(UPath filePath)
        {
            try
            {
                _selectedImageInfo.Image = new Bitmap(filePath.FullName);
                _bestImages[_selectedImageIndex] = (Image)_selectedImageInfo.Image.Clone();

                var imageFormat = _selectedImageInfo.ImageFormat;
                if (_selectedImageInfo is IndexedImageInfo indexImageInfo)
                {
                    var paletteFormat = indexImageInfo.PaletteFormat;

                    var transcoder = _selectedImageInfo.Configuration
                        .TranscodeWith(imageSize => _imageState.SupportedIndexEncodings[imageFormat])
                        .TranscodePaletteWith(() => _imageState.SupportedPaletteEncodings[paletteFormat])
                        .Build();

                    _selectedImageInfo.Image = await Task.Run(() =>
                    {
                        var (imageData, paletteData) = transcoder.Encode((Bitmap)_selectedImageInfo.Image);
                        return (Bitmap)transcoder.Decode(imageData, paletteData);
                    });
                }
                else
                {
                    var transcoder = _selectedImageInfo.Configuration.TranscodeWith(imageSize =>
                        _imageState.SupportedEncodings[imageFormat]).Build();

                    _selectedImageInfo.Image = await Task.Run(() =>
                        (Bitmap)transcoder.Decode(transcoder.Encode(_selectedImageInfo.Image)));
                }

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

        //private async Task<bool> ImageEncode(ImageInfo imageInfo, EncodingInfo imageEncoding, EncodingInfo paletteEncoding)
        //{
        //    if (!tsbFormat.Enabled && !tsbPalette.Enabled)
        //        return false;
        //    if (_imageAdapter is IIndexedImageAdapter && imageEncoding.IsIndexed && paletteEncoding == null)
        //        return false;

        //    var report = new Progress<ProgressReport>();
        //    report.ProgressChanged += Report_ProgressChanged;

        //    DisablePaletteControls();
        //    DisableImageControls();

        //    bool commitResult;
        //    try
        //    {
        //        ImageTranscodeResult result;
        //        if (_imageAdapter is IIndexedImageAdapter indexAdapter && imageEncoding.IsIndexed)
        //        {
        //            result = await indexAdapter.TranscodeImage(imageInfo, imageEncoding, paletteEncoding, report);
        //            if (!result.Result)
        //            {
        //                MessageBox.Show(result.Exception?.ToString() ?? "Encoding was not successful.",
        //                    "Encoding was not successful", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //                UpdateForm();
        //                return result.Result;
        //            }

        //            commitResult = indexAdapter.Commit(imageInfo, result.Image, imageEncoding, result.Palette, paletteEncoding);
        //        }
        //        else
        //        {
        //            result = await _imageAdapter.TranscodeImage(imageInfo, imageEncoding, report);
        //            if (!result.Result)
        //            {
        //                MessageBox.Show(result.Exception?.ToString() ?? "Encoding was not successful.",
        //                    "Encoding was not successful", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //                UpdateForm();
        //                return result.Result;
        //            }

        //            commitResult = _imageAdapter.Commit(imageInfo, result.Image, imageEncoding);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), "Exception Caught", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //        UpdateForm();
        //        return false;
        //    }

        //    UpdateForm();
        //    UpdatePreview();
        //    UpdateImageList();

        //    return commitResult;
        //}

        //private void Report_ProgressChanged(object sender, ProgressReport e)
        //{
        //    ReportProgress?.Invoke(this, e);
        //    //pbEncoding.Text = $"{(e.HasMessage ? $"{e.Message} - " : string.Empty)}{e.Percentage}%";
        //    //pbEncoding.Value = Convert.ToInt32(e.Percentage);
        //}

        private void ImageForm_Load(object sender, EventArgs e)
        {
            Dock = DockStyle.Fill;
            Padding = new Padding(3);
        }

        #region Private methods

        public void UpdateForm()
        {
            tsbSave.Enabled = _imageState is ISaveFiles;
            tsbSaveAs.Enabled = _imageState is ISaveFiles && _stateInfo.ParentStateInfo == null;

            var isIndexed = _selectedImageInfo is IndexedImageInfo;
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
                imbPreview.Image = _selectedImageInfo.Image;
                imbPreview.Zoom -= 1;
                imbPreview.Zoom += 1;
                //pptImageProperties.SelectedObject = SelectedImageInfo;
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
            tsbFormat.Text = _imageState.SupportedEncodings[_selectedImageInfo.ImageFormat].FormatName;
            tsbFormat.Tag = _selectedImageInfo.ImageFormat;

            // Update selected format
            foreach (ToolStripMenuItem tsm in tsbFormat.DropDownItems)
                tsm.Checked = (int)tsm.Tag == _selectedImageInfo.ImageFormat;

            if (_selectedImageInfo is IndexedImageInfo indexedInfo)
            {
                // Palette Dropdown
                tsbPalette.Text = _imageState.SupportedPaletteEncodings[indexedInfo.PaletteFormat].FormatName;
                tsbPalette.Tag = indexedInfo.PaletteFormat;

                // Update selected palette format
                foreach (ToolStripMenuItem tsm in tsbPalette.DropDownItems)
                    tsm.Checked = (int)tsm.Tag == indexedInfo.PaletteFormat;

                // Palette Picture Box
                var dimPalette = Convert.ToInt32(Math.Sqrt(indexedInfo.ColorCount));
                var paletteImg = ComposeImage(indexedInfo.Palette, dimPalette, dimPalette);
                if (paletteImg != null)
                    pbPalette.Image = paletteImg;
            }

            // Dimensions
            tslWidth.Text = _selectedImageInfo.ImageSize.Width.ToString();
            tslHeight.Text = _selectedImageInfo.ImageSize.Height.ToString();
        }

        private void SetGridColor(Color startColor, Action<Color> setColorToProperties)
        {
            clrDialog.Color = imbPreview.GridColor;
            if (clrDialog.ShowDialog() != DialogResult.OK)
                return;

            setColorToProperties(clrDialog.Color);

            UpdatePreview();
            GenerateThumbnailBackground();
            UpdateImageList();

            treBitmaps.SelectedNode = treBitmaps.Nodes[_selectedImageIndex];
        }

        private static Bitmap ComposeImage(IList<Color> colors, int width, int height)
        {
            var image = new Bitmap(width, height);
            BitmapData data;
            try
            {
                data = image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                return null;
            }

            unsafe
            {
                var ptr = (int*)data.Scan0;
                for (int i = 0; i < image.Width * image.Height; i++)
                {
                    if (i >= colors.Count)
                        break;
                    ptr[i] = colors[i].ToArgb();
                }
            }
            image.UnlockBits(data);

            return image;
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
                var imageInfo = _imageState.Images[i];
                if (imageInfo.Image == null) continue;
                imlBitmaps.Images.Add(i.ToString(), GenerateThumbnail(imageInfo.Image));
                treBitmaps.Nodes.Add(new TreeNode
                {
                    Text = !string.IsNullOrEmpty(imageInfo.Name) ? imageInfo.Name : i.ToString("00"),
                    Tag = i,
                    ImageKey = i.ToString(),
                    SelectedImageKey = i.ToString()
                });
            }

            treBitmaps.EndUpdate();
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
        #endregion

        private void imbPreview_Zoomed(object sender, ImageBoxZoomEventArgs e)
        {
            tslZoom.Text = "Zoom: " + imbPreview.Zoom + "%";
        }

        private void imbPreview_MouseEnter(object sender, EventArgs e)
        {
            imbPreview.Focus();
        }

        private bool _setIndexInImage;
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

        private int _paletteChosenColorIndex = -1;
        private void PbPalette_MouseClick(object sender, MouseEventArgs e)
        {
            if (_paletteChooseColor)
                _paletteChosenColorIndex = GetPaletteIndex(e.Location);
            else
                SetColorInPalette(GetPaletteIndex, e.Location);
        }

        private void ImbPreview_MouseClick(object sender, MouseEventArgs e)
        {
            if (_setIndexInImage && _paletteChosenColorIndex >= 0)
                SetIndexInImage(e.Location, _paletteChosenColorIndex);
            else
                SetColorInPalette(GetPaletteIndexByImageLocation, e.Location);
        }

        private async void SetColorInPalette(Func<Point, int> indexFunc, Point controlPoint)
        {
            if (!(_selectedImageInfo is IndexedImageInfo indexInfo))
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

            //var progress = new Progress<ProgressReport>();
            //progress.ProgressChanged += Report_ProgressChanged;
            bool commitRes;
            try
            {
                var indices = new List<int>();
                foreach (var color in Kanvas.Composition.DecomposeImage((Bitmap)indexInfo.Image, Size.Empty))
                    indices.Add(indexInfo.Palette.IndexOf(color));

                indexInfo.Palette[index] = clrDialog.Color;
                indexInfo.Image = (Bitmap)Kanvas.Composition.ComposeImage(indices.Select(x => indexInfo.Palette[x]).ToArray(), Size.Empty);

                //var result = await indexAdapter.SetColorInPalette(indexInfo, index, clrDialog.Color, progress);
                //if (!result.Result)
                //{
                //    MessageBox.Show("Setting color in palette was not successful.", "Set color unsuccessful",
                //        MessageBoxButtons.OK, MessageBoxIcon.Error);
                //    UpdateForm();
                //    return;
                //}

                //commitRes = indexAdapter.Commit(indexInfo, result.Image, indexInfo.ImageEncoding,
                //    result.Palette, indexInfo.PaletteEncoding);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Exception catched", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateForm();
                return;
            }

            //if (!commitRes)
            //{
            //    MessageBox.Show("Setting color in palette was not successful.", "Set color unsuccessful",
            //        MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    UpdateForm();
            //    return;
            //}

            // TODO: Currently reset the best bitmap to quantized image, so Encode will encode the quantized image with changed palette
            _bestImages[_selectedImageIndex] = indexInfo.Image;

            UpdateForm();
            UpdatePreview();
            UpdateImageList();
        }

        private async void SetIndexInImage(Point controlPoint, int newIndex)
        {
            if (!(_selectedImageInfo is IndexedImageInfo indexInfo))
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

            //var progress = new Progress<ProgressReport>();
            //progress.ProgressChanged += Report_ProgressChanged;
            bool commitRes;
            try
            {
                var indices = new List<int>();
                foreach (var color in Kanvas.Composition.DecomposeImage((Bitmap)indexInfo.Image, Size.Empty))
                    indices.Add(indexInfo.Palette.IndexOf(color));

                indices[pointInImg.Y * _selectedImageInfo.ImageSize.Width + pointInImg.X] = newIndex;
                indexInfo.Image = (Bitmap)Kanvas.Composition.ComposeImage(indices.Select(x => indexInfo.Palette[x]).ToArray(), Size.Empty);

                //var result = await indexAdapter.SetIndexInImage(indexInfo, pointInImg, newIndex, progress);
                //if (!result.Result)
                //{
                //    MessageBox.Show("Setting index in image was not successful.", "Set index unsuccessful",
                //        MessageBoxButtons.OK, MessageBoxIcon.Error);
                //    UpdateForm();
                //    return;
                //}

                //commitRes = indexAdapter.Commit(indexInfo, result.Image, indexInfo.ImageEncoding,
                //    result.Palette, indexInfo.PaletteEncoding);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Exception catched", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateForm();
                return;
            }

            //if (!commitRes)
            //{
            //    MessageBox.Show("Setting index in image was not successful.", "Set color unsuccessful",
            //        MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    UpdateForm();
            //    return;
            //}

            // TODO: Currently reset the best bitmap to quantized image, so Encode will encode the quantized image with changed palette
            _bestImages[_selectedImageIndex] = indexInfo.Image;

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
            var pixelColor = _selectedImageInfo.Image.GetPixel(pointInImg.X, pointInImg.Y);

            return (_selectedImageInfo as IndexedImageInfo)?.Palette.IndexOf(pixelColor) ?? -1;
        }

        private void TsbPaletteImport_Click(object sender, EventArgs e)
        {
            ImportPalette();
        }

        private async void ImportPalette()
        {
            if (!(_selectedImageInfo is IndexedImageInfo indexInfo))
                return;

            var colors = LoadPaletteFile();
            if (colors == null)
                return;

            DisablePaletteControls();
            DisableImageControls();

            //var progress = new Progress<ProgressReport>();
            //progress.ProgressChanged += Report_ProgressChanged;
            bool commitRes;
            try
            {
                var indices = new List<int>();
                foreach (var color in Kanvas.Composition.DecomposeImage((Bitmap)indexInfo.Image, Size.Empty))
                    indices.Add(indexInfo.Palette.IndexOf(color));

                indexInfo.Palette = colors;
                indexInfo.Image = (Bitmap)Kanvas.Composition.ComposeImage(indices.Select(x => indexInfo.Palette[x]).ToArray(), Size.Empty);

                //var result = await indexAdapter.SetPalette(indexInfo, colors, progress);
                //if (!result.Result)
                //{
                //    MessageBox.Show("Setting color in palette was not successful.", "Set color unsuccessful",
                //        MessageBoxButtons.OK, MessageBoxIcon.Error);
                //    UpdateForm();
                //    return;
                //}

                //commitRes = indexAdapter.Commit(indexInfo, result.Image, indexInfo.ImageEncoding,
                //    colors, indexInfo.PaletteEncoding);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Exception catched", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateForm();
                return;
            }

            //if (!commitRes)
            //{
            //    MessageBox.Show("Setting color in palette was not successful.", "Set color unsuccessful",
            //        MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    UpdateForm();
            //    return;
            //}

            // TODO: Currently reset the best bitmap to quantized image, so Encode will encode the quantized image with changed palette
            _bestImages[_selectedImageIndex] = indexInfo.Image;

            UpdateForm();
            UpdatePreview();
            UpdateImageList();
        }

        private IList<Color> LoadPaletteFile()
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Open palette...",
                InitialDirectory = Settings.Default.LastDirectory,
                Filter = "Kuriimu Palette (*.kpal)|*.kpal|Microsoft RIFF Palette (*.pal)|*.pal"
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

        private void TsbPaletteExport_Click(object sender, EventArgs e)
        {
            ExportPalette();
        }

        private void ExportPalette()
        {
            if (!(_selectedImageInfo is IndexedImageInfo indexInfo))
                return;

            SavePaletteFile(indexInfo.Palette);
        }

        private void SavePaletteFile(IList<Color> colors)
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                Title = "Save palette...",
                InitialDirectory = Settings.Default.LastDirectory,
                Filter = "Kuriimu Palette (*.kpal)|*.kpal"
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

        private void PbPalette_MouseEnter(object sender, EventArgs e)
        {
            pbPalette.Focus();
        }

        private bool _paletteChooseColor;
        private void PbPalette_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ShiftKey)
                _paletteChooseColor = true;
        }

        private void PbPalette_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ShiftKey)
                _paletteChooseColor = false;
        }
    }
}
