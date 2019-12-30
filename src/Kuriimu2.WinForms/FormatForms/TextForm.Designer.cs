//namespace Kuriimu2.WinForms.FormatForms
//{
//    partial class TextForm
//    {
//        /// <summary>
//        /// Required designer variable.
//        /// </summary>
//        private System.ComponentModel.IContainer components = null;

//        /// <summary>
//        /// Clean up any resources being used.
//        /// </summary>
//        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
//        protected override void Dispose(bool disposing)
//        {
//            if (disposing && (components != null))
//            {
//                components.Dispose();
//            }
//            base.Dispose(disposing);
//        }

//        #region Windows Form Designer generated code

//        /// <summary>
//        /// Required method for Designer support - do not modify
//        /// the contents of this method with the code editor.
//        /// </summary>
//        private void InitializeComponent()
//        {
//            this.tlsMain = new System.Windows.Forms.ToolStrip();
//            this.tlsMainSave = new System.Windows.Forms.ToolStripButton();
//            this.tlsMainSaveAs = new System.Windows.Forms.ToolStripButton();
//            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
//            this.imgPreview = new Cyotek.Windows.Forms.ImageBox();
//            this.tlsPreview = new System.Windows.Forms.ToolStrip();
//            this.tlsPreviewPlugin = new System.Windows.Forms.ToolStripDropDownButton();
//            this.tlsPreviewZoom = new System.Windows.Forms.ToolStripLabel();
//            this.tlsPreviewTool = new System.Windows.Forms.ToolStripLabel();
//            this.lstText = new System.Windows.Forms.ListView();
//            this.clmName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
//            this.clmText = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
//            this.clmOriginal = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
//            this.clmNotes = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
//            this.tlsText = new System.Windows.Forms.ToolStrip();
//            this.tlsTextAdd = new System.Windows.Forms.ToolStripButton();
//            this.tlsTextEntryCount = new System.Windows.Forms.ToolStripLabel();
//            this.tlsMain.SuspendLayout();
//            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
//            this.splitContainer1.Panel1.SuspendLayout();
//            this.splitContainer1.Panel2.SuspendLayout();
//            this.splitContainer1.SuspendLayout();
//            this.tlsPreview.SuspendLayout();
//            this.tlsText.SuspendLayout();
//            this.SuspendLayout();
//            // 
//            // tlsMain
//            // 
//            this.tlsMain.BackColor = System.Drawing.Color.Transparent;
//            this.tlsMain.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
//            this.tlsMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
//            this.tlsMainSave,
//            this.tlsMainSaveAs});
//            this.tlsMain.Location = new System.Drawing.Point(0, 0);
//            this.tlsMain.Name = "tlsMain";
//            this.tlsMain.Size = new System.Drawing.Size(800, 25);
//            this.tlsMain.TabIndex = 0;
//            // 
//            // tlsMainSave
//            // 
//            this.tlsMainSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
//            this.tlsMainSave.Enabled = false;
//            this.tlsMainSave.Image = global::Kuriimu2.WinForms.Properties.Resources.menu_save;
//            this.tlsMainSave.ImageTransparentColor = System.Drawing.Color.Magenta;
//            this.tlsMainSave.Name = "tlsMainSave";
//            this.tlsMainSave.Size = new System.Drawing.Size(23, 22);
//            this.tlsMainSave.Click += new System.EventHandler(this.tlsMainSave_Click);
//            // 
//            // tlsMainSaveAs
//            // 
//            this.tlsMainSaveAs.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
//            this.tlsMainSaveAs.Enabled = false;
//            this.tlsMainSaveAs.Image = global::Kuriimu2.WinForms.Properties.Resources.menu_save_as;
//            this.tlsMainSaveAs.ImageTransparentColor = System.Drawing.Color.Magenta;
//            this.tlsMainSaveAs.Name = "tlsMainSaveAs";
//            this.tlsMainSaveAs.Size = new System.Drawing.Size(23, 22);
//            this.tlsMainSaveAs.Text = "toolStripButton2";
//            this.tlsMainSaveAs.Click += new System.EventHandler(this.tlsMainSaveAs_Click);
//            // 
//            // splitContainer1
//            // 
//            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
//            this.splitContainer1.Location = new System.Drawing.Point(0, 25);
//            this.splitContainer1.Name = "splitContainer1";
//            // 
//            // splitContainer1.Panel1
//            // 
//            this.splitContainer1.Panel1.Controls.Add(this.imgPreview);
//            this.splitContainer1.Panel1.Controls.Add(this.tlsPreview);
//            // 
//            // splitContainer1.Panel2
//            // 
//            this.splitContainer1.Panel2.Controls.Add(this.lstText);
//            this.splitContainer1.Panel2.Controls.Add(this.tlsText);
//            this.splitContainer1.Size = new System.Drawing.Size(800, 425);
//            this.splitContainer1.SplitterDistance = 297;
//            this.splitContainer1.TabIndex = 1;
//            // 
//            // imgPreview
//            // 
//            this.imgPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
//            this.imgPreview.Dock = System.Windows.Forms.DockStyle.Fill;
//            this.imgPreview.GridCellSize = 16;
//            this.imgPreview.Location = new System.Drawing.Point(0, 25);
//            this.imgPreview.Name = "imgPreview";
//            this.imgPreview.SelectionMode = Cyotek.Windows.Forms.ImageBoxSelectionMode.Zoom;
//            this.imgPreview.Size = new System.Drawing.Size(297, 400);
//            this.imgPreview.TabIndex = 1;
//            this.imgPreview.TabStop = false;
//            this.imgPreview.Zoomed += new System.EventHandler<Cyotek.Windows.Forms.ImageBoxZoomEventArgs>(this.imgPreview_Zoomed);
//            this.imgPreview.KeyDown += new System.Windows.Forms.KeyEventHandler(this.imgPreview_KeyDown);
//            this.imgPreview.KeyUp += new System.Windows.Forms.KeyEventHandler(this.imgPreview_KeyUp);
//            // 
//            // tlsPreview
//            // 
//            this.tlsPreview.BackColor = System.Drawing.Color.Transparent;
//            this.tlsPreview.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
//            this.tlsPreview.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
//            this.tlsPreviewPlugin,
//            this.tlsPreviewZoom,
//            this.tlsPreviewTool});
//            this.tlsPreview.Location = new System.Drawing.Point(0, 0);
//            this.tlsPreview.Name = "tlsPreview";
//            this.tlsPreview.Size = new System.Drawing.Size(297, 25);
//            this.tlsPreview.TabIndex = 0;
//            this.tlsPreview.Text = "toolStrip1";
//            // 
//            // tlsPreviewPlugin
//            // 
//            this.tlsPreviewPlugin.ImageTransparentColor = System.Drawing.Color.Magenta;
//            this.tlsPreviewPlugin.Name = "tlsPreviewPlugin";
//            this.tlsPreviewPlugin.Size = new System.Drawing.Size(98, 22);
//            this.tlsPreviewPlugin.Text = "Preview plugin";
//            // 
//            // tlsPreviewZoom
//            // 
//            this.tlsPreviewZoom.Name = "tlsPreviewZoom";
//            this.tlsPreviewZoom.Size = new System.Drawing.Size(73, 22);
//            this.tlsPreviewZoom.Text = "Zoom: 100%";
//            // 
//            // tlsPreviewTool
//            // 
//            this.tlsPreviewTool.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
//            this.tlsPreviewTool.Name = "tlsPreviewTool";
//            this.tlsPreviewTool.Size = new System.Drawing.Size(68, 22);
//            this.tlsPreviewTool.Text = "Tool: Zoom";
//            // 
//            // lstText
//            // 
//            this.lstText.Activation = System.Windows.Forms.ItemActivation.OneClick;
//            this.lstText.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
//            this.clmName,
//            this.clmText,
//            this.clmOriginal,
//            this.clmNotes});
//            this.lstText.Dock = System.Windows.Forms.DockStyle.Fill;
//            this.lstText.Location = new System.Drawing.Point(0, 25);
//            this.lstText.MultiSelect = false;
//            this.lstText.Name = "lstText";
//            this.lstText.ShowGroups = false;
//            this.lstText.ShowItemToolTips = true;
//            this.lstText.Size = new System.Drawing.Size(499, 400);
//            this.lstText.TabIndex = 0;
//            this.lstText.UseCompatibleStateImageBehavior = false;
//            this.lstText.View = System.Windows.Forms.View.Details;
//            this.lstText.SelectedIndexChanged += new System.EventHandler(this.lstText_SelectedIndexChanged);
//            // 
//            // clmName
//            // 
//            this.clmName.Text = "Name";
//            this.clmName.Width = 72;
//            // 
//            // clmText
//            // 
//            this.clmText.Text = "Text";
//            this.clmText.Width = 132;
//            // 
//            // clmOriginal
//            // 
//            this.clmOriginal.Text = "Original";
//            this.clmOriginal.Width = 144;
//            // 
//            // clmNotes
//            // 
//            this.clmNotes.Text = "Notes";
//            this.clmNotes.Width = 146;
//            // 
//            // tlsText
//            // 
//            this.tlsText.BackColor = System.Drawing.Color.Transparent;
//            this.tlsText.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
//            this.tlsText.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
//            this.tlsTextAdd,
//            this.tlsTextEntryCount});
//            this.tlsText.Location = new System.Drawing.Point(0, 0);
//            this.tlsText.Name = "tlsText";
//            this.tlsText.Size = new System.Drawing.Size(499, 25);
//            this.tlsText.TabIndex = 1;
//            // 
//            // tlsTextAdd
//            // 
//            this.tlsTextAdd.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
//            this.tlsTextAdd.Enabled = false;
//            this.tlsTextAdd.Image = global::Kuriimu2.WinForms.Properties.Resources.menu_add;
//            this.tlsTextAdd.ImageTransparentColor = System.Drawing.Color.Magenta;
//            this.tlsTextAdd.Name = "tlsTextAdd";
//            this.tlsTextAdd.Size = new System.Drawing.Size(23, 22);
//            // 
//            // tlsTextEntryCount
//            // 
//            this.tlsTextEntryCount.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
//            this.tlsTextEntryCount.Name = "tlsTextEntryCount";
//            this.tlsTextEntryCount.Size = new System.Drawing.Size(54, 22);
//            this.tlsTextEntryCount.Text = "Entries: 0";
//            // 
//            // TextForm
//            // 
//            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
//            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
//            this.Controls.Add(this.splitContainer1);
//            this.Controls.Add(this.tlsMain);
//            this.Name = "TextForm";
//            this.Size = new System.Drawing.Size(800, 450);
//            this.tlsMain.ResumeLayout(false);
//            this.tlsMain.PerformLayout();
//            this.splitContainer1.Panel1.ResumeLayout(false);
//            this.splitContainer1.Panel1.PerformLayout();
//            this.splitContainer1.Panel2.ResumeLayout(false);
//            this.splitContainer1.Panel2.PerformLayout();
//            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
//            this.splitContainer1.ResumeLayout(false);
//            this.tlsPreview.ResumeLayout(false);
//            this.tlsPreview.PerformLayout();
//            this.tlsText.ResumeLayout(false);
//            this.tlsText.PerformLayout();
//            this.ResumeLayout(false);
//            this.PerformLayout();

//        }

//        #endregion

//        private System.Windows.Forms.ToolStrip tlsMain;
//        private System.Windows.Forms.ToolStripButton tlsMainSave;
//        private System.Windows.Forms.ToolStripButton tlsMainSaveAs;
//        private System.Windows.Forms.SplitContainer splitContainer1;
//        private System.Windows.Forms.ToolStrip tlsPreview;
//        private Cyotek.Windows.Forms.ImageBox imgPreview;
//        private System.Windows.Forms.ListView lstText;
//        private System.Windows.Forms.ColumnHeader clmName;
//        private System.Windows.Forms.ColumnHeader clmText;
//        private System.Windows.Forms.ColumnHeader clmOriginal;
//        private System.Windows.Forms.ColumnHeader clmNotes;
//        private System.Windows.Forms.ToolStrip tlsText;
//        private System.Windows.Forms.ToolStripButton tlsTextAdd;
//        private System.Windows.Forms.ToolStripLabel tlsTextEntryCount;
//        private System.Windows.Forms.ToolStripLabel tlsPreviewZoom;
//        private System.Windows.Forms.ToolStripLabel tlsPreviewTool;
//        private System.Windows.Forms.ToolStripDropDownButton tlsPreviewPlugin;
//    }
//}