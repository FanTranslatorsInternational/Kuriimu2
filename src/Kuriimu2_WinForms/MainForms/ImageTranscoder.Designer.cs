namespace Kuriimu2_WinForms.MainForms
{
    partial class ImageTranscoder
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.imgToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splMain = new System.Windows.Forms.SplitContainer();
            this.splImage = new System.Windows.Forms.SplitContainer();
            this.pbSource = new Cyotek.Windows.Forms.ImageBox();
            this.pbTarget = new Cyotek.Windows.Forms.ImageBox();
            this.splProperties = new System.Windows.Forms.SplitContainer();
            this.swizzleLabel = new System.Windows.Forms.Label();
            this.encLabel = new System.Windows.Forms.Label();
            this.cbSwizzle = new System.Windows.Forms.ComboBox();
            this.cbEncoding = new System.Windows.Forms.ComboBox();
            this.splExtendedProperties = new System.Windows.Forms.SplitContainer();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splMain)).BeginInit();
            this.splMain.Panel1.SuspendLayout();
            this.splMain.Panel2.SuspendLayout();
            this.splMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splImage)).BeginInit();
            this.splImage.Panel1.SuspendLayout();
            this.splImage.Panel2.SuspendLayout();
            this.splImage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splProperties)).BeginInit();
            this.splProperties.Panel1.SuspendLayout();
            this.splProperties.Panel2.SuspendLayout();
            this.splProperties.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splExtendedProperties)).BeginInit();
            this.splExtendedProperties.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.imgToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(664, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // imgToolStripMenuItem
            // 
            this.imgToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.exportToolStripMenuItem});
            this.imgToolStripMenuItem.Name = "imgToolStripMenuItem";
            this.imgToolStripMenuItem.Size = new System.Drawing.Size(52, 20);
            this.imgToolStripMenuItem.Text = "Image";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.OpenToolStripMenuItem_Click);
            // 
            // exportToolStripMenuItem
            // 
            this.exportToolStripMenuItem.Enabled = false;
            this.exportToolStripMenuItem.Name = "exportToolStripMenuItem";
            this.exportToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.exportToolStripMenuItem.Text = "Export";
            this.exportToolStripMenuItem.Click += new System.EventHandler(this.ExportToolStripMenuItem_Click);
            // 
            // splMain
            // 
            this.splMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splMain.IsSplitterFixed = true;
            this.splMain.Location = new System.Drawing.Point(0, 24);
            this.splMain.Name = "splMain";
            this.splMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splMain.Panel1
            // 
            this.splMain.Panel1.Controls.Add(this.splImage);
            // 
            // splMain.Panel2
            // 
            this.splMain.Panel2.Controls.Add(this.splProperties);
            this.splMain.Size = new System.Drawing.Size(664, 525);
            this.splMain.SplitterDistance = 448;
            this.splMain.TabIndex = 1;
            // 
            // splImage
            // 
            this.splImage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splImage.Location = new System.Drawing.Point(0, 0);
            this.splImage.Name = "splImage";
            // 
            // splImage.Panel1
            // 
            this.splImage.Panel1.Controls.Add(this.pbSource);
            this.splImage.Panel1.Padding = new System.Windows.Forms.Padding(3, 3, 0, 3);
            // 
            // splImage.Panel2
            // 
            this.splImage.Panel2.Controls.Add(this.pbTarget);
            this.splImage.Panel2.Padding = new System.Windows.Forms.Padding(0, 3, 3, 3);
            this.splImage.Size = new System.Drawing.Size(664, 448);
            this.splImage.SplitterDistance = 331;
            this.splImage.TabIndex = 0;
            // 
            // pbSource
            // 
            this.pbSource.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbSource.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbSource.GridCellSize = 16;
            this.pbSource.Location = new System.Drawing.Point(3, 3);
            this.pbSource.Name = "pbSource";
            this.pbSource.Size = new System.Drawing.Size(328, 442);
            this.pbSource.TabIndex = 0;
            // 
            // pbTarget
            // 
            this.pbTarget.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbTarget.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbTarget.GridCellSize = 16;
            this.pbTarget.Location = new System.Drawing.Point(0, 3);
            this.pbTarget.Name = "pbTarget";
            this.pbTarget.Size = new System.Drawing.Size(326, 442);
            this.pbTarget.TabIndex = 0;
            // 
            // splProperties
            // 
            this.splProperties.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splProperties.IsSplitterFixed = true;
            this.splProperties.Location = new System.Drawing.Point(0, 0);
            this.splProperties.Name = "splProperties";
            this.splProperties.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splProperties.Panel1
            // 
            this.splProperties.Panel1.Controls.Add(this.swizzleLabel);
            this.splProperties.Panel1.Controls.Add(this.encLabel);
            this.splProperties.Panel1.Controls.Add(this.cbSwizzle);
            this.splProperties.Panel1.Controls.Add(this.cbEncoding);
            // 
            // splProperties.Panel2
            // 
            this.splProperties.Panel2.Controls.Add(this.splExtendedProperties);
            this.splProperties.Size = new System.Drawing.Size(664, 73);
            this.splProperties.SplitterDistance = 25;
            this.splProperties.TabIndex = 1;
            // 
            // swizzleLabel
            // 
            this.swizzleLabel.AutoSize = true;
            this.swizzleLabel.Location = new System.Drawing.Point(170, 6);
            this.swizzleLabel.Name = "swizzleLabel";
            this.swizzleLabel.Size = new System.Drawing.Size(45, 13);
            this.swizzleLabel.TabIndex = 5;
            this.swizzleLabel.Text = "Swizzle:";
            // 
            // encLabel
            // 
            this.encLabel.AutoSize = true;
            this.encLabel.Location = new System.Drawing.Point(3, 6);
            this.encLabel.Name = "encLabel";
            this.encLabel.Size = new System.Drawing.Size(55, 13);
            this.encLabel.TabIndex = 4;
            this.encLabel.Text = "Encoding:";
            // 
            // cbSwizzle
            // 
            this.cbSwizzle.FormattingEnabled = true;
            this.cbSwizzle.Location = new System.Drawing.Point(221, 3);
            this.cbSwizzle.Name = "cbSwizzle";
            this.cbSwizzle.Size = new System.Drawing.Size(100, 21);
            this.cbSwizzle.TabIndex = 1;
            // 
            // cbEncoding
            // 
            this.cbEncoding.FormattingEnabled = true;
            this.cbEncoding.Location = new System.Drawing.Point(64, 3);
            this.cbEncoding.Name = "cbEncoding";
            this.cbEncoding.Size = new System.Drawing.Size(100, 21);
            this.cbEncoding.TabIndex = 0;
            // 
            // splExtendedProperties
            // 
            this.splExtendedProperties.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splExtendedProperties.Location = new System.Drawing.Point(0, 0);
            this.splExtendedProperties.Name = "splExtendedProperties";
            this.splExtendedProperties.Size = new System.Drawing.Size(664, 44);
            this.splExtendedProperties.SplitterDistance = 330;
            this.splExtendedProperties.TabIndex = 0;
            // 
            // ImageTranscoder
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(664, 549);
            this.Controls.Add(this.splMain);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "ImageTranscoder";
            this.Text = "Image Transcoder";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.splMain.Panel1.ResumeLayout(false);
            this.splMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splMain)).EndInit();
            this.splMain.ResumeLayout(false);
            this.splImage.Panel1.ResumeLayout(false);
            this.splImage.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splImage)).EndInit();
            this.splImage.ResumeLayout(false);
            this.splProperties.Panel1.ResumeLayout(false);
            this.splProperties.Panel1.PerformLayout();
            this.splProperties.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splProperties)).EndInit();
            this.splProperties.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splExtendedProperties)).EndInit();
            this.splExtendedProperties.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem imgToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.SplitContainer splMain;
        private System.Windows.Forms.SplitContainer splImage;
        private Cyotek.Windows.Forms.ImageBox pbSource;
        private Cyotek.Windows.Forms.ImageBox pbTarget;
        private System.Windows.Forms.SplitContainer splProperties;
        private System.Windows.Forms.Label swizzleLabel;
        private System.Windows.Forms.Label encLabel;
        private System.Windows.Forms.ComboBox cbSwizzle;
        private System.Windows.Forms.ComboBox cbEncoding;
        private System.Windows.Forms.SplitContainer splExtendedProperties;
        private System.Windows.Forms.ToolStripMenuItem exportToolStripMenuItem;
    }
}