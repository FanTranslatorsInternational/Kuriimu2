using System.Windows.Forms;

namespace Kuriimu2_WinForms.FormatForms
{
    partial class ImageForm
    {
        /// <summary> 
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Komponenten-Designer generierter Code

        /// <summary> 
        /// Erforderliche Methode für die Designerunterstützung. 
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.tlsMain = new System.Windows.Forms.ToolStrip();
            this.tsbSave = new System.Windows.Forms.ToolStripButton();
            this.tsbSaveAs = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbExport = new System.Windows.Forms.ToolStripButton();
            this.tsbImport = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbGridColor1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsbGridColor2 = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsbImageBorderStyle = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsbImageBorderColor = new System.Windows.Forms.ToolStripDropDownButton();
            this.pnlMain = new System.Windows.Forms.Panel();
            this.splMain = new System.Windows.Forms.SplitContainer();
            this.imbPreview = new Cyotek.Windows.Forms.ImageBox();
            this.cmsPreview = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tlsTools = new System.Windows.Forms.ToolStrip();
            this.tslZoom = new System.Windows.Forms.ToolStripLabel();
            this.tslTool = new System.Windows.Forms.ToolStripLabel();
            this.splProperties = new System.Windows.Forms.SplitContainer();
            this.treBitmaps = new System.Windows.Forms.TreeView();
            this.imlBitmaps = new System.Windows.Forms.ImageList(this.components);
            this.pptImageProperties = new System.Windows.Forms.PropertyGrid();
            this.tlsProperties = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.clrDialog = new System.Windows.Forms.ColorDialog();
            this.tlsMain.SuspendLayout();
            this.pnlMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splMain)).BeginInit();
            this.splMain.Panel1.SuspendLayout();
            this.splMain.Panel2.SuspendLayout();
            this.splMain.SuspendLayout();
            this.tlsTools.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splProperties)).BeginInit();
            this.splProperties.Panel1.SuspendLayout();
            this.splProperties.Panel2.SuspendLayout();
            this.splProperties.SuspendLayout();
            this.tlsProperties.SuspendLayout();
            this.SuspendLayout();
            // 
            // tlsMain
            // 
            this.tlsMain.BackColor = System.Drawing.Color.Transparent;
            this.tlsMain.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tlsMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbSave,
            this.tsbSaveAs,
            this.toolStripSeparator1,
            this.tsbExport,
            this.tsbImport,
            this.toolStripSeparator2,
            this.tsbGridColor1,
            this.tsbGridColor2,
            this.tsbImageBorderStyle,
            this.tsbImageBorderColor});
            this.tlsMain.Location = new System.Drawing.Point(0, 0);
            this.tlsMain.Name = "tlsMain";
            this.tlsMain.Size = new System.Drawing.Size(789, 25);
            this.tlsMain.TabIndex = 1;
            this.tlsMain.Text = "toolStrip1";
            // 
            // tsbSave
            // 
            this.tsbSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbSave.Enabled = false;
            this.tsbSave.Image = global::Kuriimu2_WinForms.Properties.Resources.menu_save;
            this.tsbSave.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbSave.Name = "tsbSave";
            this.tsbSave.Size = new System.Drawing.Size(23, 22);
            this.tsbSave.Click += new System.EventHandler(this.tsbSave_Click);
            // 
            // tsbSaveAs
            // 
            this.tsbSaveAs.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbSaveAs.Enabled = false;
            this.tsbSaveAs.Image = global::Kuriimu2_WinForms.Properties.Resources.menu_save_as;
            this.tsbSaveAs.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbSaveAs.Name = "tsbSaveAs";
            this.tsbSaveAs.Size = new System.Drawing.Size(23, 22);
            this.tsbSaveAs.Click += new System.EventHandler(this.tsbSaveAs_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // tsbExport
            // 
            this.tsbExport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbExport.Image = global::Kuriimu2_WinForms.Properties.Resources.image_export;
            this.tsbExport.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbExport.Name = "tsbExport";
            this.tsbExport.Size = new System.Drawing.Size(23, 22);
            this.tsbExport.Click += new System.EventHandler(this.tsbExport_Click);
            // 
            // tsbImport
            // 
            this.tsbImport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbImport.Image = global::Kuriimu2_WinForms.Properties.Resources.import_import;
            this.tsbImport.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbImport.Name = "tsbImport";
            this.tsbImport.Size = new System.Drawing.Size(23, 22);
            this.tsbImport.Click += new System.EventHandler(this.tsbImport_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // tsbGridColor1
            // 
            this.tsbGridColor1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbGridColor1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbGridColor1.Name = "tsbGridColor1";
            this.tsbGridColor1.Size = new System.Drawing.Size(13, 22);
            this.tsbGridColor1.Text = "Grid Color 1";
            this.tsbGridColor1.Click += new System.EventHandler(this.tsbGridColor1_Click);
            // 
            // tsbGridColor2
            // 
            this.tsbGridColor2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbGridColor2.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbGridColor2.Name = "tsbGridColor2";
            this.tsbGridColor2.Size = new System.Drawing.Size(13, 22);
            this.tsbGridColor2.Text = "Grid Color 2";
            this.tsbGridColor2.Click += new System.EventHandler(this.tsbGridColor2_Click);
            // 
            // tsbImageBorderStyle
            // 
            this.tsbImageBorderStyle.Image = global::Kuriimu2_WinForms.Properties.Resources.menu_border_none;
            this.tsbImageBorderStyle.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbImageBorderStyle.Name = "tsbImageBorderStyle";
            this.tsbImageBorderStyle.Size = new System.Drawing.Size(65, 22);
            this.tsbImageBorderStyle.Text = "None";
            this.tsbImageBorderStyle.ToolTipText = "Image Border Style";
            // 
            // tsbImageBorderColor
            // 
            this.tsbImageBorderColor.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbImageBorderColor.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbImageBorderColor.Name = "tsbImageBorderColor";
            this.tsbImageBorderColor.Size = new System.Drawing.Size(13, 22);
            this.tsbImageBorderColor.Text = "Image Border Color";
            this.tsbImageBorderColor.Click += new System.EventHandler(this.tsbImageBorderColor_Click);
            // 
            // pnlMain
            // 
            this.pnlMain.Controls.Add(this.splMain);
            this.pnlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMain.Location = new System.Drawing.Point(0, 25);
            this.pnlMain.Name = "pnlMain";
            this.pnlMain.Size = new System.Drawing.Size(789, 446);
            this.pnlMain.TabIndex = 2;
            // 
            // splMain
            // 
            this.splMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splMain.Location = new System.Drawing.Point(0, 0);
            this.splMain.Name = "splMain";
            // 
            // splMain.Panel1
            // 
            this.splMain.Panel1.Controls.Add(this.imbPreview);
            this.splMain.Panel1.Controls.Add(this.tlsTools);
            // 
            // splMain.Panel2
            // 
            this.splMain.Panel2.Controls.Add(this.splProperties);
            this.splMain.Size = new System.Drawing.Size(789, 446);
            this.splMain.SplitterDistance = 570;
            this.splMain.TabIndex = 1;
            // 
            // imbPreview
            // 
            this.imbPreview.AllowDrop = true;
            this.imbPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.imbPreview.ContextMenuStrip = this.cmsPreview;
            this.imbPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imbPreview.GridCellSize = 16;
            this.imbPreview.GridColor = System.Drawing.Color.Silver;
            this.imbPreview.ImageBorderColor = System.Drawing.Color.Black;
            this.imbPreview.ImageBorderStyle = Cyotek.Windows.Forms.ImageBoxBorderStyle.FixedSingleDropShadow;
            this.imbPreview.Location = new System.Drawing.Point(0, 0);
            this.imbPreview.Name = "imbPreview";
            this.imbPreview.SelectionMode = Cyotek.Windows.Forms.ImageBoxSelectionMode.Zoom;
            this.imbPreview.Size = new System.Drawing.Size(570, 421);
            this.imbPreview.TabIndex = 0;
            this.imbPreview.TabStop = false;
            this.imbPreview.Zoomed += new System.EventHandler<Cyotek.Windows.Forms.ImageBoxZoomEventArgs>(this.imbPreview_Zoomed);
            this.imbPreview.DragDrop += new System.Windows.Forms.DragEventHandler(this.imbPreview_DragDrop);
            this.imbPreview.DragEnter += new System.Windows.Forms.DragEventHandler(this.imbPreview_DragEnter);
            this.imbPreview.KeyDown += new System.Windows.Forms.KeyEventHandler(this.imbPreview_KeyDown);
            this.imbPreview.KeyUp += new System.Windows.Forms.KeyEventHandler(this.imbPreview_KeyUp);
            this.imbPreview.MouseEnter += new System.EventHandler(this.imbPreview_MouseEnter);
            // 
            // cmsPreview
            // 
            this.cmsPreview.Name = "cmsPreview";
            this.cmsPreview.Size = new System.Drawing.Size(61, 4);
            // 
            // tlsTools
            // 
            this.tlsTools.AutoSize = false;
            this.tlsTools.BackColor = System.Drawing.Color.Transparent;
            this.tlsTools.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tlsTools.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tlsTools.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tslZoom,
            this.tslTool});
            this.tlsTools.Location = new System.Drawing.Point(0, 421);
            this.tlsTools.Name = "tlsTools";
            this.tlsTools.Size = new System.Drawing.Size(570, 25);
            this.tlsTools.TabIndex = 3;
            // 
            // tslZoom
            // 
            this.tslZoom.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tslZoom.Name = "tslZoom";
            this.tslZoom.Size = new System.Drawing.Size(73, 22);
            this.tslZoom.Text = "Zoom: 100%";
            // 
            // tslTool
            // 
            this.tslTool.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tslTool.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tslTool.Name = "tslTool";
            this.tslTool.Size = new System.Drawing.Size(68, 22);
            this.tslTool.Text = "Tool: Zoom";
            // 
            // splProperties
            // 
            this.splProperties.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splProperties.Location = new System.Drawing.Point(0, 0);
            this.splProperties.Name = "splProperties";
            this.splProperties.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splProperties.Panel1
            // 
            this.splProperties.Panel1.Controls.Add(this.treBitmaps);
            // 
            // splProperties.Panel2
            // 
            this.splProperties.Panel2.Controls.Add(this.pptImageProperties);
            this.splProperties.Panel2.Controls.Add(this.tlsProperties);
            this.splProperties.Size = new System.Drawing.Size(215, 446);
            this.splProperties.SplitterDistance = 236;
            this.splProperties.TabIndex = 0;
            // 
            // treBitmaps
            // 
            this.treBitmaps.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treBitmaps.FullRowSelect = true;
            this.treBitmaps.HideSelection = false;
            this.treBitmaps.ImageIndex = 0;
            this.treBitmaps.ImageList = this.imlBitmaps;
            this.treBitmaps.Location = new System.Drawing.Point(0, 0);
            this.treBitmaps.Name = "treBitmaps";
            this.treBitmaps.SelectedImageIndex = 0;
            this.treBitmaps.ShowLines = false;
            this.treBitmaps.ShowPlusMinus = false;
            this.treBitmaps.ShowRootLines = false;
            this.treBitmaps.Size = new System.Drawing.Size(215, 236);
            this.treBitmaps.TabIndex = 0;
            this.treBitmaps.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treBitmaps_AfterSelect);
            this.treBitmaps.MouseEnter += new System.EventHandler(this.treBitmaps_MouseEnter);
            // 
            // imlBitmaps
            // 
            this.imlBitmaps.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this.imlBitmaps.ImageSize = new System.Drawing.Size(94, 64);
            this.imlBitmaps.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // pptImageProperties
            // 
            this.pptImageProperties.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pptImageProperties.LineColor = System.Drawing.SystemColors.ControlDark;
            this.pptImageProperties.Location = new System.Drawing.Point(0, 25);
            this.pptImageProperties.Name = "pptImageProperties";
            this.pptImageProperties.Size = new System.Drawing.Size(215, 181);
            this.pptImageProperties.TabIndex = 1;
            this.pptImageProperties.ToolbarVisible = false;
            // 
            // tlsProperties
            // 
            this.tlsProperties.AutoSize = false;
            this.tlsProperties.BackColor = System.Drawing.Color.Transparent;
            this.tlsProperties.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tlsProperties.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel1});
            this.tlsProperties.Location = new System.Drawing.Point(0, 0);
            this.tlsProperties.Name = "tlsProperties";
            this.tlsProperties.Size = new System.Drawing.Size(215, 25);
            this.tlsProperties.TabIndex = 0;
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(60, 22);
            this.toolStripLabel1.Text = "Properties";
            // 
            // ImageForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pnlMain);
            this.Controls.Add(this.tlsMain);
            this.Name = "ImageForm";
            this.Size = new System.Drawing.Size(789, 471);
            this.Load += new System.EventHandler(this.ImageForm_Load);
            this.tlsMain.ResumeLayout(false);
            this.tlsMain.PerformLayout();
            this.pnlMain.ResumeLayout(false);
            this.splMain.Panel1.ResumeLayout(false);
            this.splMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splMain)).EndInit();
            this.splMain.ResumeLayout(false);
            this.tlsTools.ResumeLayout(false);
            this.tlsTools.PerformLayout();
            this.splProperties.Panel1.ResumeLayout(false);
            this.splProperties.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splProperties)).EndInit();
            this.splProperties.ResumeLayout(false);
            this.tlsProperties.ResumeLayout(false);
            this.tlsProperties.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ToolStrip tlsMain;
        private System.Windows.Forms.Panel pnlMain;
        private Cyotek.Windows.Forms.ImageBox imbPreview;
        private SplitContainer splMain;
        private ContextMenuStrip cmsPreview;
        private TreeView treBitmaps;
        private SplitContainer splProperties;
        private PropertyGrid pptImageProperties;
        private ToolStrip tlsProperties;
        private ToolStripLabel toolStripLabel1;
        private ToolStripButton tsbSave;
        private ToolStripButton tsbSaveAs;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripButton tsbExport;
        private ToolStripButton tsbImport;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripDropDownButton tsbGridColor1;
        private ToolStripDropDownButton tsbGridColor2;
        private ToolStripDropDownButton tsbImageBorderStyle;
        private ToolStripDropDownButton tsbImageBorderColor;
        private ColorDialog clrDialog;
        private ImageList imlBitmaps;
        private ToolStrip tlsTools;
        private ToolStripLabel tslZoom;
        private ToolStripLabel tslTool;
    }
}
