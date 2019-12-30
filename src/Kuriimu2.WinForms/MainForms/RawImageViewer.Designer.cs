//namespace Kuriimu2.WinForms.MainForms
//{
//    partial class RawImageViewer
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
//            this.mnuMain = new System.Windows.Forms.MenuStrip();
//            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
//            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
//            this.splMain = new System.Windows.Forms.SplitContainer();
//            this.pbMain = new Cyotek.Windows.Forms.ImageBox();
//            this.tslPbProperties = new System.Windows.Forms.ToolStrip();
//            this.tslZoom = new System.Windows.Forms.ToolStripLabel();
//            this.tslPbWidth = new System.Windows.Forms.ToolStripLabel();
//            this.tslPbWidthLabel = new System.Windows.Forms.ToolStripLabel();
//            this.tslPbHeight = new System.Windows.Forms.ToolStripLabel();
//            this.tslPbHeightLabel = new System.Windows.Forms.ToolStripLabel();
//            this.btnDecode = new System.Windows.Forms.Button();
//            this.splProperties = new System.Windows.Forms.SplitContainer();
//            this.tbHeight = new System.Windows.Forms.TextBox();
//            this.tbWidth = new System.Windows.Forms.TextBox();
//            this.heightLabel = new System.Windows.Forms.Label();
//            this.widthLabel = new System.Windows.Forms.Label();
//            this.swizzleLabel = new System.Windows.Forms.Label();
//            this.encLabel = new System.Windows.Forms.Label();
//            this.offsetLabel = new System.Windows.Forms.Label();
//            this.tbOffset = new System.Windows.Forms.TextBox();
//            this.cbSwizzle = new System.Windows.Forms.ComboBox();
//            this.cbEncoding = new System.Windows.Forms.ComboBox();
//            this.splExtendedProperties = new System.Windows.Forms.SplitContainer();
//            this.mnuMain.SuspendLayout();
//            ((System.ComponentModel.ISupportInitialize)(this.splMain)).BeginInit();
//            this.splMain.Panel1.SuspendLayout();
//            this.splMain.Panel2.SuspendLayout();
//            this.splMain.SuspendLayout();
//            this.tslPbProperties.SuspendLayout();
//            ((System.ComponentModel.ISupportInitialize)(this.splProperties)).BeginInit();
//            this.splProperties.Panel1.SuspendLayout();
//            this.splProperties.Panel2.SuspendLayout();
//            this.splProperties.SuspendLayout();
//            ((System.ComponentModel.ISupportInitialize)(this.splExtendedProperties)).BeginInit();
//            this.splExtendedProperties.SuspendLayout();
//            this.SuspendLayout();
//            // 
//            // mnuMain
//            // 
//            this.mnuMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
//            this.fileToolStripMenuItem});
//            this.mnuMain.Location = new System.Drawing.Point(0, 0);
//            this.mnuMain.Name = "mnuMain";
//            this.mnuMain.Size = new System.Drawing.Size(522, 24);
//            this.mnuMain.TabIndex = 0;
//            // 
//            // fileToolStripMenuItem
//            // 
//            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
//            this.openToolStripMenuItem});
//            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
//            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
//            this.fileToolStripMenuItem.Text = "File";
//            // 
//            // openToolStripMenuItem
//            // 
//            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
//            this.openToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
//            this.openToolStripMenuItem.Text = "Open";
//            this.openToolStripMenuItem.Click += new System.EventHandler(this.OpenToolStripMenuItem_Click);
//            // 
//            // splMain
//            // 
//            this.splMain.Dock = System.Windows.Forms.DockStyle.Fill;
//            this.splMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
//            this.splMain.Location = new System.Drawing.Point(0, 24);
//            this.splMain.Name = "splMain";
//            this.splMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
//            // 
//            // splMain.Panel1
//            // 
//            this.splMain.Panel1.Controls.Add(this.pbMain);
//            this.splMain.Panel1.Controls.Add(this.tslPbProperties);
//            this.splMain.Panel1.Controls.Add(this.btnDecode);
//            this.splMain.Panel1.Padding = new System.Windows.Forms.Padding(5);
//            // 
//            // splMain.Panel2
//            // 
//            this.splMain.Panel2.Controls.Add(this.splProperties);
//            this.splMain.Size = new System.Drawing.Size(522, 628);
//            this.splMain.SplitterDistance = 469;
//            this.splMain.TabIndex = 1;
//            // 
//            // pbMain
//            // 
//            this.pbMain.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
//            this.pbMain.Dock = System.Windows.Forms.DockStyle.Fill;
//            this.pbMain.GridCellSize = 16;
//            this.pbMain.Location = new System.Drawing.Point(5, 5);
//            this.pbMain.Name = "pbMain";
//            this.pbMain.Size = new System.Drawing.Size(512, 411);
//            this.pbMain.TabIndex = 0;
//            this.pbMain.ZoomChanged += new System.EventHandler(this.PbMain_ZoomChanged);
//            // 
//            // tslPbProperties
//            // 
//            this.tslPbProperties.BackColor = System.Drawing.Color.Transparent;
//            this.tslPbProperties.Dock = System.Windows.Forms.DockStyle.Bottom;
//            this.tslPbProperties.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
//            this.tslPbProperties.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
//            this.tslZoom,
//            this.tslPbWidth,
//            this.tslPbWidthLabel,
//            this.tslPbHeight,
//            this.tslPbHeightLabel});
//            this.tslPbProperties.Location = new System.Drawing.Point(5, 416);
//            this.tslPbProperties.Name = "tslPbProperties";
//            this.tslPbProperties.Size = new System.Drawing.Size(512, 25);
//            this.tslPbProperties.TabIndex = 2;
//            this.tslPbProperties.Text = "toolStrip1";
//            // 
//            // tslZoom
//            // 
//            this.tslZoom.Name = "tslZoom";
//            this.tslZoom.Size = new System.Drawing.Size(73, 22);
//            this.tslZoom.Text = "Zoom: 100%";
//            // 
//            // tslPbWidth
//            // 
//            this.tslPbWidth.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
//            this.tslPbWidth.Name = "tslPbWidth";
//            this.tslPbWidth.Size = new System.Drawing.Size(0, 22);
//            // 
//            // tslPbWidthLabel
//            // 
//            this.tslPbWidthLabel.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
//            this.tslPbWidthLabel.Name = "tslPbWidthLabel";
//            this.tslPbWidthLabel.Size = new System.Drawing.Size(42, 22);
//            this.tslPbWidthLabel.Text = "Width:";
//            // 
//            // tslPbHeight
//            // 
//            this.tslPbHeight.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
//            this.tslPbHeight.Name = "tslPbHeight";
//            this.tslPbHeight.Size = new System.Drawing.Size(0, 22);
//            // 
//            // tslPbHeightLabel
//            // 
//            this.tslPbHeightLabel.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
//            this.tslPbHeightLabel.Name = "tslPbHeightLabel";
//            this.tslPbHeightLabel.Size = new System.Drawing.Size(46, 22);
//            this.tslPbHeightLabel.Text = "Height:";
//            // 
//            // btnDecode
//            // 
//            this.btnDecode.Dock = System.Windows.Forms.DockStyle.Bottom;
//            this.btnDecode.Location = new System.Drawing.Point(5, 441);
//            this.btnDecode.Name = "btnDecode";
//            this.btnDecode.Size = new System.Drawing.Size(512, 23);
//            this.btnDecode.TabIndex = 1;
//            this.btnDecode.Text = "Decode";
//            this.btnDecode.UseVisualStyleBackColor = true;
//            this.btnDecode.Click += new System.EventHandler(this.BtnDecode_Click);
//            // 
//            // splProperties
//            // 
//            this.splProperties.Dock = System.Windows.Forms.DockStyle.Fill;
//            this.splProperties.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
//            this.splProperties.Location = new System.Drawing.Point(0, 0);
//            this.splProperties.Name = "splProperties";
//            this.splProperties.Orientation = System.Windows.Forms.Orientation.Horizontal;
//            // 
//            // splProperties.Panel1
//            // 
//            this.splProperties.Panel1.Controls.Add(this.tbHeight);
//            this.splProperties.Panel1.Controls.Add(this.tbWidth);
//            this.splProperties.Panel1.Controls.Add(this.heightLabel);
//            this.splProperties.Panel1.Controls.Add(this.widthLabel);
//            this.splProperties.Panel1.Controls.Add(this.swizzleLabel);
//            this.splProperties.Panel1.Controls.Add(this.encLabel);
//            this.splProperties.Panel1.Controls.Add(this.offsetLabel);
//            this.splProperties.Panel1.Controls.Add(this.tbOffset);
//            this.splProperties.Panel1.Controls.Add(this.cbSwizzle);
//            this.splProperties.Panel1.Controls.Add(this.cbEncoding);
//            // 
//            // splProperties.Panel2
//            // 
//            this.splProperties.Panel2.Controls.Add(this.splExtendedProperties);
//            this.splProperties.Size = new System.Drawing.Size(522, 155);
//            this.splProperties.SplitterDistance = 55;
//            this.splProperties.TabIndex = 0;
//            // 
//            // tbHeight
//            // 
//            this.tbHeight.Location = new System.Drawing.Point(221, 30);
//            this.tbHeight.Name = "tbHeight";
//            this.tbHeight.Size = new System.Drawing.Size(100, 20);
//            this.tbHeight.TabIndex = 11;
//            this.tbHeight.Text = "1";
//            // 
//            // tbWidth
//            // 
//            this.tbWidth.Location = new System.Drawing.Point(64, 30);
//            this.tbWidth.Name = "tbWidth";
//            this.tbWidth.Size = new System.Drawing.Size(100, 20);
//            this.tbWidth.TabIndex = 10;
//            this.tbWidth.Text = "1";
//            // 
//            // heightLabel
//            // 
//            this.heightLabel.AutoSize = true;
//            this.heightLabel.Location = new System.Drawing.Point(170, 33);
//            this.heightLabel.Name = "heightLabel";
//            this.heightLabel.Size = new System.Drawing.Size(41, 13);
//            this.heightLabel.TabIndex = 9;
//            this.heightLabel.Text = "Height:";
//            // 
//            // widthLabel
//            // 
//            this.widthLabel.AutoSize = true;
//            this.widthLabel.Location = new System.Drawing.Point(3, 33);
//            this.widthLabel.Name = "widthLabel";
//            this.widthLabel.Size = new System.Drawing.Size(38, 13);
//            this.widthLabel.TabIndex = 7;
//            this.widthLabel.Text = "Width:";
//            // 
//            // swizzleLabel
//            // 
//            this.swizzleLabel.AutoSize = true;
//            this.swizzleLabel.Location = new System.Drawing.Point(170, 6);
//            this.swizzleLabel.Name = "swizzleLabel";
//            this.swizzleLabel.Size = new System.Drawing.Size(45, 13);
//            this.swizzleLabel.TabIndex = 5;
//            this.swizzleLabel.Text = "Swizzle:";
//            // 
//            // encLabel
//            // 
//            this.encLabel.AutoSize = true;
//            this.encLabel.Location = new System.Drawing.Point(3, 6);
//            this.encLabel.Name = "encLabel";
//            this.encLabel.Size = new System.Drawing.Size(55, 13);
//            this.encLabel.TabIndex = 4;
//            this.encLabel.Text = "Encoding:";
//            // 
//            // offsetLabel
//            // 
//            this.offsetLabel.AutoSize = true;
//            this.offsetLabel.Location = new System.Drawing.Point(327, 6);
//            this.offsetLabel.Name = "offsetLabel";
//            this.offsetLabel.Size = new System.Drawing.Size(54, 13);
//            this.offsetLabel.TabIndex = 3;
//            this.offsetLabel.Text = "FileOffset:";
//            // 
//            // tbOffset
//            // 
//            this.tbOffset.Location = new System.Drawing.Point(387, 3);
//            this.tbOffset.Name = "tbOffset";
//            this.tbOffset.Size = new System.Drawing.Size(100, 20);
//            this.tbOffset.TabIndex = 2;
//            this.tbOffset.Text = "0";
//            // 
//            // cbSwizzle
//            // 
//            this.cbSwizzle.FormattingEnabled = true;
//            this.cbSwizzle.Location = new System.Drawing.Point(221, 3);
//            this.cbSwizzle.Name = "cbSwizzle";
//            this.cbSwizzle.Size = new System.Drawing.Size(100, 21);
//            this.cbSwizzle.TabIndex = 1;
//            this.cbSwizzle.SelectedIndexChanged += new System.EventHandler(this.CbSwizzle_SelectedIndexChanged);
//            // 
//            // cbEncoding
//            // 
//            this.cbEncoding.FormattingEnabled = true;
//            this.cbEncoding.Location = new System.Drawing.Point(64, 3);
//            this.cbEncoding.Name = "cbEncoding";
//            this.cbEncoding.Size = new System.Drawing.Size(100, 21);
//            this.cbEncoding.TabIndex = 0;
//            this.cbEncoding.SelectedIndexChanged += new System.EventHandler(this.CbEncoding_SelectedIndexChanged);
//            // 
//            // splExtendedProperties
//            // 
//            this.splExtendedProperties.Dock = System.Windows.Forms.DockStyle.Fill;
//            this.splExtendedProperties.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
//            this.splExtendedProperties.Location = new System.Drawing.Point(0, 0);
//            this.splExtendedProperties.Name = "splExtendedProperties";
//            this.splExtendedProperties.Orientation = System.Windows.Forms.Orientation.Horizontal;
//            this.splExtendedProperties.Size = new System.Drawing.Size(522, 96);
//            this.splExtendedProperties.SplitterDistance = 48;
//            this.splExtendedProperties.TabIndex = 0;
//            // 
//            // RawImageViewer
//            // 
//            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
//            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
//            this.ClientSize = new System.Drawing.Size(522, 652);
//            this.Controls.Add(this.splMain);
//            this.Controls.Add(this.mnuMain);
//            this.MainMenuStrip = this.mnuMain;
//            this.Name = "RawImageViewer";
//            this.Text = "Raw Image Viewer";
//            this.mnuMain.ResumeLayout(false);
//            this.mnuMain.PerformLayout();
//            this.splMain.Panel1.ResumeLayout(false);
//            this.splMain.Panel1.PerformLayout();
//            this.splMain.Panel2.ResumeLayout(false);
//            ((System.ComponentModel.ISupportInitialize)(this.splMain)).EndInit();
//            this.splMain.ResumeLayout(false);
//            this.tslPbProperties.ResumeLayout(false);
//            this.tslPbProperties.PerformLayout();
//            this.splProperties.Panel1.ResumeLayout(false);
//            this.splProperties.Panel1.PerformLayout();
//            this.splProperties.Panel2.ResumeLayout(false);
//            ((System.ComponentModel.ISupportInitialize)(this.splProperties)).EndInit();
//            this.splProperties.ResumeLayout(false);
//            ((System.ComponentModel.ISupportInitialize)(this.splExtendedProperties)).EndInit();
//            this.splExtendedProperties.ResumeLayout(false);
//            this.ResumeLayout(false);
//            this.PerformLayout();

//        }

//        #endregion

//        private System.Windows.Forms.MenuStrip mnuMain;
//        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
//        private System.Windows.Forms.SplitContainer splMain;
//        private Cyotek.Windows.Forms.ImageBox pbMain;
//        private System.Windows.Forms.SplitContainer splProperties;
//        private System.Windows.Forms.Label swizzleLabel;
//        private System.Windows.Forms.Label encLabel;
//        private System.Windows.Forms.Label offsetLabel;
//        private System.Windows.Forms.TextBox tbOffset;
//        private System.Windows.Forms.ComboBox cbSwizzle;
//        private System.Windows.Forms.ComboBox cbEncoding;
//        private System.Windows.Forms.SplitContainer splExtendedProperties;
//        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
//        private System.Windows.Forms.TextBox tbHeight;
//        private System.Windows.Forms.TextBox tbWidth;
//        private System.Windows.Forms.Label heightLabel;
//        private System.Windows.Forms.Label widthLabel;
//        private System.Windows.Forms.Button btnDecode;
//        private System.Windows.Forms.ToolStrip tslPbProperties;
//        private System.Windows.Forms.ToolStripLabel tslZoom;
//        private System.Windows.Forms.ToolStripLabel tslPbWidth;
//        private System.Windows.Forms.ToolStripLabel tslPbWidthLabel;
//        private System.Windows.Forms.ToolStripLabel tslPbHeight;
//        private System.Windows.Forms.ToolStripLabel tslPbHeightLabel;
//    }
//}