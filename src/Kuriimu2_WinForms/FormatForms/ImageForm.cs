using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kuriimu2_WinForms.Interfaces;
using Kore;
using Kontract.Interfaces.Archive;
using Kontract.Interfaces.Image;
using Kuriimu2_WinForms.Properties;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using Cyotek.Windows.Forms;
using Kontract.Interfaces.Common;
using System.IO;
using Kontract;

namespace Kuriimu2_WinForms.FormatForms
{
    public partial class ImageForm : UserControl, IKuriimuForm
    {
        private IArchiveAdapter _parentAdapter;
        private TabPage _currentTab;
        private TabPage _parentTab;

        private IImageAdapter _imageAdapter => (IImageAdapter)Kfi.Adapter;
        private int _selectedImageIndex = 0;
        private Bitmap _thumbnailBackground;

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

        public ImageForm(KoreFileInfo kfi, TabPage tabPage, IArchiveAdapter parentAdapter, TabPage parentTabPage)
        {
            InitializeComponent();

            Kfi = kfi;

            _currentTab = tabPage;
            _parentTab = parentTabPage;
            _parentAdapter = parentAdapter;

            imbPreview.Image = _imageAdapter.BitmapInfos.FirstOrDefault()?.Image;

            tsbImageBorderStyle.DropDownItems.AddRange(Enum.GetNames(typeof(ImageBoxBorderStyle)).Select(s => new ToolStripMenuItem { Image = (Image)Resources.ResourceManager.GetObject(_stylesImages[s]), Text = _stylesText[s], Tag = s }).ToArray());
            foreach (var tsb in tsbImageBorderStyle.DropDownItems)
                ((ToolStripMenuItem)tsb).Click += tsbImageBorderStyle_Click;

            UpdateForm();
            UpdatePreview();
            UpdateImageList();
        }

        public KoreFileInfo Kfi { get; set; }
        public Color TabColor { get; set; }

        public event EventHandler<SaveTabEventArgs> SaveTab;
        public event EventHandler<CloseTabEventArgs> CloseTab;

        public void Close()
        {
            CloseTab?.Invoke(this, new CloseTabEventArgs(Kfi) { LeaveOpen = Kfi.ParentKfi != null });
        }

        private void SaveAs()
        {
            var sfd = new SaveFileDialog();
            sfd.FileName = Path.GetFileName(Kfi.StreamFileInfo.FileName);
            sfd.Filter = "All Files (*.*)|*.*";

            if (sfd.ShowDialog() == DialogResult.OK)
                Save(sfd.FileName);
            else
                MessageBox.Show("No save location was chosen", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public void Save(string filename = "")
        {
            SaveTab?.Invoke(this, new SaveTabEventArgs(Kfi) { NewSaveFile = filename });

            UpdateParent();
            UpdateForm();
        }

        private void ExportPng()
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                Title = "Export PNG...",
                InitialDirectory = Settings.Default.LastDirectory,
                FileName = Path.GetFileNameWithoutExtension(Kfi.StreamFileInfo.FileName) + "." + _selectedImageIndex.ToString("00") + ".png",
                Filter = "Portable Network Graphics (*.png)|*.png",
                AddExtension = true
            };

            if (sfd.ShowDialog() == DialogResult.OK)
                _imageAdapter.BitmapInfos[_selectedImageIndex].Image.Save(sfd.FileName, ImageFormat.Png);
        }

        private void ImportPng()
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Import PNG...",
                InitialDirectory = Settings.Default.LastDirectory,
                Filter = "Portable Network Graphics (*.png)|*.png"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
                Import(ofd.FileName);
        }

        private void Import(string filename)
        {
            try
            {
                _imageAdapter.BitmapInfos[_selectedImageIndex].Image = new Bitmap(filename);
                // TODO: Use progress report on UI
                _imageAdapter.Encode(_imageAdapter.BitmapInfos[_selectedImageIndex], new Progress<ProgressReport>());

                UpdatePreview();
                UpdateImageList();
                treBitmaps.SelectedNode = treBitmaps.Nodes[_selectedImageIndex];
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void UpdateParent()
        {
            if (_parentTab != null)
                if (_parentTab.Controls[0] is IArchiveForm archiveForm)
                {
                    archiveForm.UpdateForm();
                    archiveForm.UpdateParent();
                }
        }

        public void UpdateForm()
        {
            _currentTab.Text = Kfi.DisplayName;

            tsbSave.Enabled = _imageAdapter is ISaveFiles;
            tsbSaveAs.Enabled = _imageAdapter is ISaveFiles && Kfi.ParentKfi == null;
        }

        private void ImageForm_Load(object sender, EventArgs e)
        {
            Dock = DockStyle.Fill;
            Padding = new Padding(3);
        }

        #region Events
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

        #region Private methods
        private void SetGridColor(Color startColor, Action<Color> setColorToProperties)
        {
            clrDialog.Color = imbPreview.GridColor;
            if (clrDialog.ShowDialog() != DialogResult.OK) return;

            setColorToProperties(clrDialog.Color);

            UpdatePreview();
            GenerateThumbnailBackground();
            UpdateImageList();

            treBitmaps.SelectedNode = treBitmaps.Nodes[_selectedImageIndex];
        }

        private void UpdatePreview()
        {
            if (_imageAdapter.BitmapInfos.Count > 0)
            {
                imbPreview.Image = _imageAdapter.BitmapInfos[_selectedImageIndex].Image;
                pptImageProperties.SelectedObject = _imageAdapter.BitmapInfos[_selectedImageIndex];
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
            if (_imageAdapter.BitmapInfos.Count <= 0) return;

            treBitmaps.BeginUpdate();
            treBitmaps.Nodes.Clear();
            imlBitmaps.Images.Clear();
            imlBitmaps.TransparentColor = Color.Transparent;
            imlBitmaps.ImageSize = new Size(Settings.Default.ThumbnailWidth, Settings.Default.ThumbnailHeight);
            treBitmaps.ItemHeight = Settings.Default.ThumbnailHeight + 6;

            for (var i = 0; i < _imageAdapter.BitmapInfos.Count; i++)
            {
                var bitmapInfo = _imageAdapter.BitmapInfos[i];
                if (bitmapInfo.Image == null) continue;
                imlBitmaps.Images.Add(i.ToString(), GenerateThumbnail(bitmapInfo.Image));
                treBitmaps.Nodes.Add(new TreeNode
                {
                    Text = !string.IsNullOrEmpty(bitmapInfo.Name) ? bitmapInfo.Name : i.ToString("00"),
                    Tag = i,
                    ImageKey = i.ToString(),
                    SelectedImageKey = i.ToString()
                });
            }

            treBitmaps.EndUpdate();
        }

        private Bitmap GenerateThumbnail(Bitmap input)
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

        private void imbPreview_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                imbPreview.SelectionMode = ImageBoxSelectionMode.None;
                imbPreview.Cursor = Cursors.SizeAll;
                tslTool.Text = "Tool: Pan";
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
    }
}
