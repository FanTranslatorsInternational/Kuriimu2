namespace Kuriimu2_WinForms
{
    partial class RawImageViewer
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
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splMain = new System.Windows.Forms.SplitContainer();
            this.pbMain = new Cyotek.Windows.Forms.ImageBox();
            this.splProperties = new System.Windows.Forms.SplitContainer();
            this.swizzleLabel = new System.Windows.Forms.Label();
            this.encLabel = new System.Windows.Forms.Label();
            this.offsetLabel = new System.Windows.Forms.Label();
            this.tbOffset = new System.Windows.Forms.TextBox();
            this.cbSwizzle = new System.Windows.Forms.ComboBox();
            this.cbEncoding = new System.Windows.Forms.ComboBox();
            this.splExtendedProperties = new System.Windows.Forms.SplitContainer();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.widthLabel = new System.Windows.Forms.Label();
            this.heightLabel = new System.Windows.Forms.Label();
            this.tbWidth = new System.Windows.Forms.TextBox();
            this.tbHeight = new System.Windows.Forms.TextBox();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splMain)).BeginInit();
            this.splMain.Panel1.SuspendLayout();
            this.splMain.Panel2.SuspendLayout();
            this.splMain.SuspendLayout();
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
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(453, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
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
            this.splMain.Panel1.Controls.Add(this.pbMain);
            this.splMain.Panel1.Padding = new System.Windows.Forms.Padding(5);
            // 
            // splMain.Panel2
            // 
            this.splMain.Panel2.Controls.Add(this.splProperties);
            this.splMain.Size = new System.Drawing.Size(453, 525);
            this.splMain.SplitterDistance = 386;
            this.splMain.TabIndex = 1;
            // 
            // pbMain
            // 
            this.pbMain.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbMain.GridCellSize = 16;
            this.pbMain.Location = new System.Drawing.Point(5, 5);
            this.pbMain.Name = "pbMain";
            this.pbMain.Size = new System.Drawing.Size(443, 376);
            this.pbMain.TabIndex = 0;
            // 
            // splProperties
            // 
            this.splProperties.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splProperties.Location = new System.Drawing.Point(0, 0);
            this.splProperties.Name = "splProperties";
            // 
            // splProperties.Panel1
            // 
            this.splProperties.Panel1.Controls.Add(this.tbHeight);
            this.splProperties.Panel1.Controls.Add(this.tbWidth);
            this.splProperties.Panel1.Controls.Add(this.heightLabel);
            this.splProperties.Panel1.Controls.Add(this.widthLabel);
            this.splProperties.Panel1.Controls.Add(this.swizzleLabel);
            this.splProperties.Panel1.Controls.Add(this.encLabel);
            this.splProperties.Panel1.Controls.Add(this.offsetLabel);
            this.splProperties.Panel1.Controls.Add(this.tbOffset);
            this.splProperties.Panel1.Controls.Add(this.cbSwizzle);
            this.splProperties.Panel1.Controls.Add(this.cbEncoding);
            // 
            // splProperties.Panel2
            // 
            this.splProperties.Panel2.Controls.Add(this.splExtendedProperties);
            this.splProperties.Size = new System.Drawing.Size(453, 135);
            this.splProperties.SplitterDistance = 174;
            this.splProperties.TabIndex = 0;
            // 
            // swizzleLabel
            // 
            this.swizzleLabel.AutoSize = true;
            this.swizzleLabel.Location = new System.Drawing.Point(3, 33);
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
            // offsetLabel
            // 
            this.offsetLabel.AutoSize = true;
            this.offsetLabel.Location = new System.Drawing.Point(3, 60);
            this.offsetLabel.Name = "offsetLabel";
            this.offsetLabel.Size = new System.Drawing.Size(54, 13);
            this.offsetLabel.TabIndex = 3;
            this.offsetLabel.Text = "FileOffset:";
            // 
            // tbOffset
            // 
            this.tbOffset.Location = new System.Drawing.Point(71, 57);
            this.tbOffset.Name = "tbOffset";
            this.tbOffset.Size = new System.Drawing.Size(100, 20);
            this.tbOffset.TabIndex = 2;
            this.tbOffset.Text = "0";
            this.tbOffset.TextChanged += new System.EventHandler(this.TbOffset_TextChanged);
            // 
            // cbSwizzle
            // 
            this.cbSwizzle.FormattingEnabled = true;
            this.cbSwizzle.Location = new System.Drawing.Point(71, 30);
            this.cbSwizzle.Name = "cbSwizzle";
            this.cbSwizzle.Size = new System.Drawing.Size(100, 21);
            this.cbSwizzle.TabIndex = 1;
            this.cbSwizzle.SelectedIndexChanged += new System.EventHandler(this.CbSwizzle_SelectedIndexChanged);
            // 
            // cbEncoding
            // 
            this.cbEncoding.FormattingEnabled = true;
            this.cbEncoding.Location = new System.Drawing.Point(71, 3);
            this.cbEncoding.Name = "cbEncoding";
            this.cbEncoding.Size = new System.Drawing.Size(100, 21);
            this.cbEncoding.TabIndex = 0;
            this.cbEncoding.SelectedIndexChanged += new System.EventHandler(this.CbEncoding_SelectedIndexChanged);
            // 
            // splExtendedProperties
            // 
            this.splExtendedProperties.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splExtendedProperties.Location = new System.Drawing.Point(0, 0);
            this.splExtendedProperties.Name = "splExtendedProperties";
            this.splExtendedProperties.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.splExtendedProperties.Size = new System.Drawing.Size(275, 135);
            this.splExtendedProperties.SplitterDistance = 65;
            this.splExtendedProperties.TabIndex = 0;
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.OpenToolStripMenuItem_Click);
            // 
            // widthLabel
            // 
            this.widthLabel.AutoSize = true;
            this.widthLabel.Location = new System.Drawing.Point(3, 86);
            this.widthLabel.Name = "widthLabel";
            this.widthLabel.Size = new System.Drawing.Size(38, 13);
            this.widthLabel.TabIndex = 7;
            this.widthLabel.Text = "Width:";
            // 
            // heightLabel
            // 
            this.heightLabel.AutoSize = true;
            this.heightLabel.Location = new System.Drawing.Point(2, 112);
            this.heightLabel.Name = "heightLabel";
            this.heightLabel.Size = new System.Drawing.Size(41, 13);
            this.heightLabel.TabIndex = 9;
            this.heightLabel.Text = "Height:";
            // 
            // tbWidth
            // 
            this.tbWidth.Location = new System.Drawing.Point(71, 83);
            this.tbWidth.Name = "tbWidth";
            this.tbWidth.Size = new System.Drawing.Size(100, 20);
            this.tbWidth.TabIndex = 10;
            this.tbWidth.Text = "1";
            this.tbWidth.TextChanged += new System.EventHandler(this.TbWidth_TextChanged);
            // 
            // tbHeight
            // 
            this.tbHeight.Location = new System.Drawing.Point(71, 109);
            this.tbHeight.Name = "tbHeight";
            this.tbHeight.Size = new System.Drawing.Size(100, 20);
            this.tbHeight.TabIndex = 11;
            this.tbHeight.Text = "1";
            this.tbHeight.TextChanged += new System.EventHandler(this.TbHeight_TextChanged);
            // 
            // RawImageViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(453, 549);
            this.Controls.Add(this.splMain);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "RawImageViewer";
            this.Text = "Raw Image Viewer";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.splMain.Panel1.ResumeLayout(false);
            this.splMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splMain)).EndInit();
            this.splMain.ResumeLayout(false);
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
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.SplitContainer splMain;
        private Cyotek.Windows.Forms.ImageBox pbMain;
        private System.Windows.Forms.SplitContainer splProperties;
        private System.Windows.Forms.Label swizzleLabel;
        private System.Windows.Forms.Label encLabel;
        private System.Windows.Forms.Label offsetLabel;
        private System.Windows.Forms.TextBox tbOffset;
        private System.Windows.Forms.ComboBox cbSwizzle;
        private System.Windows.Forms.ComboBox cbEncoding;
        private System.Windows.Forms.SplitContainer splExtendedProperties;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.TextBox tbHeight;
        private System.Windows.Forms.TextBox tbWidth;
        private System.Windows.Forms.Label heightLabel;
        private System.Windows.Forms.Label widthLabel;
    }
}