//namespace Kuriimu2.WinForms.MainForms
//{
//    partial class ImageTranscoder
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
//            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
//            this.imgToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
//            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
//            this.exportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
//            this.splMain = new System.Windows.Forms.SplitContainer();
//            this.pnlImage = new System.Windows.Forms.Panel();
//            this.splImage = new System.Windows.Forms.SplitContainer();
//            this.pbSource = new Cyotek.Windows.Forms.ImageBox();
//            this.tslPbPropertiesSource = new System.Windows.Forms.ToolStrip();
//            this.tslZoomSource = new System.Windows.Forms.ToolStripLabel();
//            this.tslPbWidthSource = new System.Windows.Forms.ToolStripLabel();
//            this.tslPbWidthLabelSource = new System.Windows.Forms.ToolStripLabel();
//            this.tslPbHeightSource = new System.Windows.Forms.ToolStripLabel();
//            this.tslPbHeightLabelSource = new System.Windows.Forms.ToolStripLabel();
//            this.pbTarget = new Cyotek.Windows.Forms.ImageBox();
//            this.tslPbPropertiesTarget = new System.Windows.Forms.ToolStrip();
//            this.tslZoomTarget = new System.Windows.Forms.ToolStripLabel();
//            this.tslPbWidthTarget = new System.Windows.Forms.ToolStripLabel();
//            this.tslPbWidthLabelTarget = new System.Windows.Forms.ToolStripLabel();
//            this.tslPbHeightTarget = new System.Windows.Forms.ToolStripLabel();
//            this.tslPbHeightLabelTarget = new System.Windows.Forms.ToolStripLabel();
//            this.btnTranscode = new System.Windows.Forms.Button();
//            this.splProperties = new System.Windows.Forms.SplitContainer();
//            this.swizzleLabel = new System.Windows.Forms.Label();
//            this.encLabel = new System.Windows.Forms.Label();
//            this.cbSwizzle = new System.Windows.Forms.ComboBox();
//            this.cbEncoding = new System.Windows.Forms.ComboBox();
//            this.splExtendedProperties = new System.Windows.Forms.SplitContainer();
//            this.menuStrip1.SuspendLayout();
//            ((System.ComponentModel.ISupportInitialize)(this.splMain)).BeginInit();
//            this.splMain.Panel1.SuspendLayout();
//            this.splMain.Panel2.SuspendLayout();
//            this.splMain.SuspendLayout();
//            this.pnlImage.SuspendLayout();
//            ((System.ComponentModel.ISupportInitialize)(this.splImage)).BeginInit();
//            this.splImage.Panel1.SuspendLayout();
//            this.splImage.Panel2.SuspendLayout();
//            this.splImage.SuspendLayout();
//            this.tslPbPropertiesSource.SuspendLayout();
//            this.tslPbPropertiesTarget.SuspendLayout();
//            ((System.ComponentModel.ISupportInitialize)(this.splProperties)).BeginInit();
//            this.splProperties.Panel1.SuspendLayout();
//            this.splProperties.Panel2.SuspendLayout();
//            this.splProperties.SuspendLayout();
//            ((System.ComponentModel.ISupportInitialize)(this.splExtendedProperties)).BeginInit();
//            this.splExtendedProperties.SuspendLayout();
//            this.SuspendLayout();
//            // 
//            // menuStrip1
//            // 
//            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
//            this.imgToolStripMenuItem});
//            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
//            this.menuStrip1.Name = "menuStrip1";
//            this.menuStrip1.Size = new System.Drawing.Size(664, 24);
//            this.menuStrip1.TabIndex = 0;
//            this.menuStrip1.Text = "menuStrip1";
//            // 
//            // imgToolStripMenuItem
//            // 
//            this.imgToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
//            this.openToolStripMenuItem,
//            this.exportToolStripMenuItem});
//            this.imgToolStripMenuItem.Name = "imgToolStripMenuItem";
//            this.imgToolStripMenuItem.Size = new System.Drawing.Size(52, 20);
//            this.imgToolStripMenuItem.Text = "Image";
//            // 
//            // openToolStripMenuItem
//            // 
//            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
//            this.openToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
//            this.openToolStripMenuItem.Text = "Open";
//            this.openToolStripMenuItem.Click += new System.EventHandler(this.OpenToolStripMenuItem_Click);
//            // 
//            // exportToolStripMenuItem
//            // 
//            this.exportToolStripMenuItem.Enabled = false;
//            this.exportToolStripMenuItem.Name = "exportToolStripMenuItem";
//            this.exportToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
//            this.exportToolStripMenuItem.Text = "Export";
//            this.exportToolStripMenuItem.Click += new System.EventHandler(this.ExportToolStripMenuItem_Click);
//            // 
//            // splMain
//            // 
//            this.splMain.Dock = System.Windows.Forms.DockStyle.Fill;
//            this.splMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
//            this.splMain.Location = new System.Drawing.Point(0, 24);
//            this.splMain.Name = "splMain";
//            this.splMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
//            // 
//            // splMain.Panel1
//            // 
//            this.splMain.Panel1.Controls.Add(this.pnlImage);
//            // 
//            // splMain.Panel2
//            // 
//            this.splMain.Panel2.Controls.Add(this.splProperties);
//            this.splMain.Size = new System.Drawing.Size(664, 547);
//            this.splMain.SplitterDistance = 415;
//            this.splMain.TabIndex = 1;
//            // 
//            // pnlImage
//            // 
//            this.pnlImage.Controls.Add(this.splImage);
//            this.pnlImage.Controls.Add(this.btnTranscode);
//            this.pnlImage.Dock = System.Windows.Forms.DockStyle.Fill;
//            this.pnlImage.Location = new System.Drawing.Point(0, 0);
//            this.pnlImage.Name = "pnlImage";
//            this.pnlImage.Size = new System.Drawing.Size(664, 415);
//            this.pnlImage.TabIndex = 1;
//            // 
//            // splImage
//            // 
//            this.splImage.Dock = System.Windows.Forms.DockStyle.Fill;
//            this.splImage.Location = new System.Drawing.Point(0, 0);
//            this.splImage.Name = "splImage";
//            // 
//            // splImage.Panel1
//            // 
//            this.splImage.Panel1.Controls.Add(this.pbSource);
//            this.splImage.Panel1.Controls.Add(this.tslPbPropertiesSource);
//            this.splImage.Panel1.Padding = new System.Windows.Forms.Padding(3, 3, 0, 3);
//            // 
//            // splImage.Panel2
//            // 
//            this.splImage.Panel2.Controls.Add(this.pbTarget);
//            this.splImage.Panel2.Controls.Add(this.tslPbPropertiesTarget);
//            this.splImage.Panel2.Padding = new System.Windows.Forms.Padding(0, 3, 3, 3);
//            this.splImage.Size = new System.Drawing.Size(664, 392);
//            this.splImage.SplitterDistance = 331;
//            this.splImage.TabIndex = 0;
//            // 
//            // pbSource
//            // 
//            this.pbSource.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
//            this.pbSource.Dock = System.Windows.Forms.DockStyle.Fill;
//            this.pbSource.GridCellSize = 16;
//            this.pbSource.Location = new System.Drawing.Point(3, 3);
//            this.pbSource.Name = "pbSource";
//            this.pbSource.Size = new System.Drawing.Size(328, 361);
//            this.pbSource.TabIndex = 0;
//            this.pbSource.ZoomChanged += new System.EventHandler(this.PbSource_ZoomChanged);
//            // 
//            // tslPbPropertiesSource
//            // 
//            this.tslPbPropertiesSource.BackColor = System.Drawing.Color.Transparent;
//            this.tslPbPropertiesSource.Dock = System.Windows.Forms.DockStyle.Bottom;
//            this.tslPbPropertiesSource.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
//            this.tslPbPropertiesSource.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
//            this.tslZoomSource,
//            this.tslPbWidthSource,
//            this.tslPbWidthLabelSource,
//            this.tslPbHeightSource,
//            this.tslPbHeightLabelSource});
//            this.tslPbPropertiesSource.Location = new System.Drawing.Point(3, 364);
//            this.tslPbPropertiesSource.Name = "tslPbPropertiesSource";
//            this.tslPbPropertiesSource.Size = new System.Drawing.Size(328, 25);
//            this.tslPbPropertiesSource.TabIndex = 3;
//            this.tslPbPropertiesSource.Text = "toolStrip1";
//            // 
//            // tslZoomSource
//            // 
//            this.tslZoomSource.Name = "tslZoomSource";
//            this.tslZoomSource.Size = new System.Drawing.Size(73, 22);
//            this.tslZoomSource.Text = "Zoom: 100%";
//            // 
//            // tslPbWidthSource
//            // 
//            this.tslPbWidthSource.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
//            this.tslPbWidthSource.Name = "tslPbWidthSource";
//            this.tslPbWidthSource.Size = new System.Drawing.Size(0, 22);
//            // 
//            // tslPbWidthLabelSource
//            // 
//            this.tslPbWidthLabelSource.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
//            this.tslPbWidthLabelSource.Name = "tslPbWidthLabelSource";
//            this.tslPbWidthLabelSource.Size = new System.Drawing.Size(42, 22);
//            this.tslPbWidthLabelSource.Text = "Width:";
//            // 
//            // tslPbHeightSource
//            // 
//            this.tslPbHeightSource.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
//            this.tslPbHeightSource.Name = "tslPbHeightSource";
//            this.tslPbHeightSource.Size = new System.Drawing.Size(0, 22);
//            // 
//            // tslPbHeightLabelSource
//            // 
//            this.tslPbHeightLabelSource.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
//            this.tslPbHeightLabelSource.Name = "tslPbHeightLabelSource";
//            this.tslPbHeightLabelSource.Size = new System.Drawing.Size(46, 22);
//            this.tslPbHeightLabelSource.Text = "Height:";
//            // 
//            // pbTarget
//            // 
//            this.pbTarget.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
//            this.pbTarget.Dock = System.Windows.Forms.DockStyle.Fill;
//            this.pbTarget.GridCellSize = 16;
//            this.pbTarget.Location = new System.Drawing.Point(0, 3);
//            this.pbTarget.Name = "pbTarget";
//            this.pbTarget.Size = new System.Drawing.Size(326, 361);
//            this.pbTarget.TabIndex = 0;
//            this.pbTarget.ZoomChanged += new System.EventHandler(this.PbTarget_ZoomChanged);
//            // 
//            // tslPbPropertiesTarget
//            // 
//            this.tslPbPropertiesTarget.BackColor = System.Drawing.Color.Transparent;
//            this.tslPbPropertiesTarget.Dock = System.Windows.Forms.DockStyle.Bottom;
//            this.tslPbPropertiesTarget.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
//            this.tslPbPropertiesTarget.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
//            this.tslZoomTarget,
//            this.tslPbWidthTarget,
//            this.tslPbWidthLabelTarget,
//            this.tslPbHeightTarget,
//            this.tslPbHeightLabelTarget});
//            this.tslPbPropertiesTarget.Location = new System.Drawing.Point(0, 364);
//            this.tslPbPropertiesTarget.Name = "tslPbPropertiesTarget";
//            this.tslPbPropertiesTarget.Size = new System.Drawing.Size(326, 25);
//            this.tslPbPropertiesTarget.TabIndex = 3;
//            this.tslPbPropertiesTarget.Text = "toolStrip1";
//            // 
//            // tslZoomTarget
//            // 
//            this.tslZoomTarget.Name = "tslZoomTarget";
//            this.tslZoomTarget.Size = new System.Drawing.Size(73, 22);
//            this.tslZoomTarget.Text = "Zoom: 100%";
//            // 
//            // tslPbWidthTarget
//            // 
//            this.tslPbWidthTarget.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
//            this.tslPbWidthTarget.Name = "tslPbWidthTarget";
//            this.tslPbWidthTarget.Size = new System.Drawing.Size(0, 22);
//            // 
//            // tslPbWidthLabelTarget
//            // 
//            this.tslPbWidthLabelTarget.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
//            this.tslPbWidthLabelTarget.Name = "tslPbWidthLabelTarget";
//            this.tslPbWidthLabelTarget.Size = new System.Drawing.Size(42, 22);
//            this.tslPbWidthLabelTarget.Text = "Width:";
//            // 
//            // tslPbHeightTarget
//            // 
//            this.tslPbHeightTarget.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
//            this.tslPbHeightTarget.Name = "tslPbHeightTarget";
//            this.tslPbHeightTarget.Size = new System.Drawing.Size(0, 22);
//            // 
//            // tslPbHeightLabelTarget
//            // 
//            this.tslPbHeightLabelTarget.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
//            this.tslPbHeightLabelTarget.Name = "tslPbHeightLabelTarget";
//            this.tslPbHeightLabelTarget.Size = new System.Drawing.Size(46, 22);
//            this.tslPbHeightLabelTarget.Text = "Height:";
//            // 
//            // btnTranscode
//            // 
//            this.btnTranscode.Dock = System.Windows.Forms.DockStyle.Bottom;
//            this.btnTranscode.Location = new System.Drawing.Point(0, 392);
//            this.btnTranscode.Name = "btnTranscode";
//            this.btnTranscode.Size = new System.Drawing.Size(664, 23);
//            this.btnTranscode.TabIndex = 1;
//            this.btnTranscode.Text = "Transcode";
//            this.btnTranscode.UseVisualStyleBackColor = true;
//            this.btnTranscode.Click += new System.EventHandler(this.BtnTranscode_Click);
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
//            this.splProperties.Panel1.Controls.Add(this.swizzleLabel);
//            this.splProperties.Panel1.Controls.Add(this.encLabel);
//            this.splProperties.Panel1.Controls.Add(this.cbSwizzle);
//            this.splProperties.Panel1.Controls.Add(this.cbEncoding);
//            // 
//            // splProperties.Panel2
//            // 
//            this.splProperties.Panel2.Controls.Add(this.splExtendedProperties);
//            this.splProperties.Size = new System.Drawing.Size(664, 128);
//            this.splProperties.SplitterDistance = 28;
//            this.splProperties.TabIndex = 1;
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
//            // cbSwizzle
//            // 
//            this.cbSwizzle.FormattingEnabled = true;
//            this.cbSwizzle.Location = new System.Drawing.Point(221, 3);
//            this.cbSwizzle.Name = "cbSwizzle";
//            this.cbSwizzle.Size = new System.Drawing.Size(100, 21);
//            this.cbSwizzle.TabIndex = 1;
//            // 
//            // cbEncoding
//            // 
//            this.cbEncoding.FormattingEnabled = true;
//            this.cbEncoding.Location = new System.Drawing.Point(64, 3);
//            this.cbEncoding.Name = "cbEncoding";
//            this.cbEncoding.Size = new System.Drawing.Size(100, 21);
//            this.cbEncoding.TabIndex = 0;
//            // 
//            // splExtendedProperties
//            // 
//            this.splExtendedProperties.Dock = System.Windows.Forms.DockStyle.Fill;
//            this.splExtendedProperties.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
//            this.splExtendedProperties.Location = new System.Drawing.Point(0, 0);
//            this.splExtendedProperties.Name = "splExtendedProperties";
//            this.splExtendedProperties.Orientation = System.Windows.Forms.Orientation.Horizontal;
//            this.splExtendedProperties.Size = new System.Drawing.Size(664, 96);
//            this.splExtendedProperties.SplitterDistance = 48;
//            this.splExtendedProperties.TabIndex = 0;
//            // 
//            // ImageTranscoder
//            // 
//            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
//            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
//            this.ClientSize = new System.Drawing.Size(664, 571);
//            this.Controls.Add(this.splMain);
//            this.Controls.Add(this.menuStrip1);
//            this.MainMenuStrip = this.menuStrip1;
//            this.Name = "ImageTranscoder";
//            this.Text = "Image Transcoder";
//            this.menuStrip1.ResumeLayout(false);
//            this.menuStrip1.PerformLayout();
//            this.splMain.Panel1.ResumeLayout(false);
//            this.splMain.Panel2.ResumeLayout(false);
//            ((System.ComponentModel.ISupportInitialize)(this.splMain)).EndInit();
//            this.splMain.ResumeLayout(false);
//            this.pnlImage.ResumeLayout(false);
//            this.splImage.Panel1.ResumeLayout(false);
//            this.splImage.Panel1.PerformLayout();
//            this.splImage.Panel2.ResumeLayout(false);
//            this.splImage.Panel2.PerformLayout();
//            ((System.ComponentModel.ISupportInitialize)(this.splImage)).EndInit();
//            this.splImage.ResumeLayout(false);
//            this.tslPbPropertiesSource.ResumeLayout(false);
//            this.tslPbPropertiesSource.PerformLayout();
//            this.tslPbPropertiesTarget.ResumeLayout(false);
//            this.tslPbPropertiesTarget.PerformLayout();
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

//        private System.Windows.Forms.MenuStrip menuStrip1;
//        private System.Windows.Forms.ToolStripMenuItem imgToolStripMenuItem;
//        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
//        private System.Windows.Forms.SplitContainer splMain;
//        private System.Windows.Forms.SplitContainer splImage;
//        private Cyotek.Windows.Forms.ImageBox pbSource;
//        private Cyotek.Windows.Forms.ImageBox pbTarget;
//        private System.Windows.Forms.SplitContainer splProperties;
//        private System.Windows.Forms.Label swizzleLabel;
//        private System.Windows.Forms.Label encLabel;
//        private System.Windows.Forms.ComboBox cbSwizzle;
//        private System.Windows.Forms.ComboBox cbEncoding;
//        private System.Windows.Forms.SplitContainer splExtendedProperties;
//        private System.Windows.Forms.ToolStripMenuItem exportToolStripMenuItem;
//        private System.Windows.Forms.Panel pnlImage;
//        private System.Windows.Forms.Button btnTranscode;
//        private System.Windows.Forms.ToolStrip tslPbPropertiesSource;
//        private System.Windows.Forms.ToolStripLabel tslZoomSource;
//        private System.Windows.Forms.ToolStripLabel tslPbWidthSource;
//        private System.Windows.Forms.ToolStripLabel tslPbWidthLabelSource;
//        private System.Windows.Forms.ToolStripLabel tslPbHeightSource;
//        private System.Windows.Forms.ToolStripLabel tslPbHeightLabelSource;
//        private System.Windows.Forms.ToolStrip tslPbPropertiesTarget;
//        private System.Windows.Forms.ToolStripLabel tslZoomTarget;
//        private System.Windows.Forms.ToolStripLabel tslPbWidthTarget;
//        private System.Windows.Forms.ToolStripLabel tslPbWidthLabelTarget;
//        private System.Windows.Forms.ToolStripLabel tslPbHeightTarget;
//        private System.Windows.Forms.ToolStripLabel tslPbHeightLabelTarget;
//    }
//}