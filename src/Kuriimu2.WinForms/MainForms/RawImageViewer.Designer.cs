using System.Drawing;
using System.Windows.Forms;

namespace Kuriimu2.WinForms.MainForms
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
            this.mnuMain = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeToolStripMenuItem= new System.Windows.Forms.ToolStripMenuItem();
            this.splMain = new System.Windows.Forms.SplitContainer();
            this.pbMain = new Cyotek.Windows.Forms.ImageBox();
            this.tslPbProperties = new System.Windows.Forms.ToolStrip();
            this.tslZoom = new System.Windows.Forms.ToolStripLabel();
            this.tslWidthLabel = new System.Windows.Forms.ToolStripLabel();
            this.tslWidth = new System.Windows.Forms.ToolStripLabel();
            this.tslHeightLabel = new System.Windows.Forms.ToolStripLabel();
            this.tslHeight = new System.Windows.Forms.ToolStripLabel();
            this.splConfiguration = new System.Windows.Forms.SplitContainer();
            this.btnProcess = new System.Windows.Forms.Button();
            this.tbWidth = new System.Windows.Forms.TextBox();
            this.widthLabel = new System.Windows.Forms.Label();
            this.tbHeight = new System.Windows.Forms.TextBox();
            this.cbEncoding = new System.Windows.Forms.ComboBox();
            this.tbOffset = new System.Windows.Forms.TextBox();
            this.cbSwizzle = new System.Windows.Forms.ComboBox();
            this.heightLabel = new System.Windows.Forms.Label();
            this.offsetLabel = new System.Windows.Forms.Label();
            this.encLabel = new System.Windows.Forms.Label();
            this.swizzleLabel = new System.Windows.Forms.Label();
            this.splParameters = new System.Windows.Forms.SplitContainer();
            this.gbEncParameters = new System.Windows.Forms.GroupBox();
            this.gbSwizzleParameters = new System.Windows.Forms.GroupBox();
            this.mnuMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splMain)).BeginInit();
            this.splMain.Panel1.SuspendLayout();
            this.splMain.Panel2.SuspendLayout();
            this.splMain.SuspendLayout();
            this.tslPbProperties.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splConfiguration)).BeginInit();
            this.splConfiguration.Panel1.SuspendLayout();
            this.splConfiguration.Panel2.SuspendLayout();
            this.splConfiguration.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splParameters)).BeginInit();
            this.splParameters.Panel1.SuspendLayout();
            this.splParameters.Panel2.SuspendLayout();
            this.splParameters.SuspendLayout();
            this.SuspendLayout();
            // 
            // mnuMain
            // 
            this.mnuMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.mnuMain.Location = new System.Drawing.Point(0, 0);
            this.mnuMain.Name = "mnuMain";
            this.mnuMain.Size = new System.Drawing.Size(604, 24);
            this.mnuMain.TabIndex = 0;
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.closeToolStripMenuItem
            });
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.OpenToolStripMenuItem_Click);
            // 
            // closeToolStripMenuItem
            // 
            this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            this.closeToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.closeToolStripMenuItem.Text = "Close";
            this.closeToolStripMenuItem.Enabled = false;
            this.closeToolStripMenuItem.Click += new System.EventHandler(this.CloseToolStripMenuItem_Click);
            // 
            // splMain
            // 
            this.splMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splMain.IsSplitterFixed = true;
            this.splMain.Location = new System.Drawing.Point(0, 24);
            this.splMain.Name = "splMain";
            this.splMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splMain.Panel1
            // 
            this.splMain.Panel1.Controls.Add(this.pbMain);
            this.splMain.Panel1.Controls.Add(this.tslPbProperties);
            this.splMain.Panel1.Padding = new System.Windows.Forms.Padding(5);
            // 
            // splMain.Panel2
            // 
            this.splMain.Panel2.Controls.Add(this.splConfiguration);
            this.splMain.Size = new System.Drawing.Size(604, 602);
            this.splMain.SplitterDistance = 431;
            this.splMain.TabIndex = 1;
            // 
            // pbMain
            // 
            this.pbMain.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbMain.GridCellSize = 16;
            this.pbMain.Location = new System.Drawing.Point(5, 5);
            this.pbMain.Name = "pbMain";
            this.pbMain.Size = new System.Drawing.Size(594, 396);
            this.pbMain.TabIndex = 0;
            this.pbMain.ZoomChanged += new System.EventHandler(this.PbMain_ZoomChanged);
            // 
            // tslPbProperties
            // 
            this.tslPbProperties.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tslPbProperties.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tslZoom,
            this.tslWidthLabel,
            this.tslWidth,
            this.tslHeightLabel,
            this.tslHeight});
            this.tslPbProperties.Location = new System.Drawing.Point(5, 401);
            this.tslPbProperties.Name = "tslPbProperties";
            this.tslPbProperties.Size = new System.Drawing.Size(594, 25);
            this.tslPbProperties.TabIndex = 1;
            this.tslPbProperties.Text = "tslPbProperties";
            // 
            // tslZoom
            // 
            this.tslZoom.Name = "tslZoom";
            this.tslZoom.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.tslZoom.Size = new System.Drawing.Size(73, 22);
            this.tslZoom.Text = "Zoom: 100%";
            // 
            // tslWidthLabel
            // 
            this.tslWidthLabel.Name = "tslWidthLabel";
            this.tslWidthLabel.Size = new System.Drawing.Size(42, 22);
            this.tslWidthLabel.Text = "Width:";
            // 
            // tslWidth
            // 
            this.tslWidth.Name = "tslWidth";
            this.tslWidth.Size = new System.Drawing.Size(13, 22);
            this.tslWidth.Text = "0";
            // 
            // tslHeightLabel
            // 
            this.tslHeightLabel.Name = "tslHeightLabel";
            this.tslHeightLabel.Size = new System.Drawing.Size(46, 22);
            this.tslHeightLabel.Text = "Height:";
            // 
            // tslHeight
            // 
            this.tslHeight.Name = "tslHeight";
            this.tslHeight.Size = new System.Drawing.Size(13, 22);
            this.tslHeight.Text = "0";
            // 
            // splConfiguration
            // 
            this.splConfiguration.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splConfiguration.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splConfiguration.Location = new System.Drawing.Point(0, 0);
            this.splConfiguration.Name = "splConfiguration";
            this.splConfiguration.Size = new System.Drawing.Size(604, 167);
            this.splConfiguration.SplitterDistance = 200;
            this.splConfiguration.IsSplitterFixed = true;
            this.splConfiguration.TabIndex = 0;
            // 
            // splConfiguration.Panel1
            // 
            this.splConfiguration.Panel1.Controls.Add(this.btnProcess);
            this.splConfiguration.Panel1.Controls.Add(this.tbWidth);
            this.splConfiguration.Panel1.Controls.Add(this.widthLabel);
            this.splConfiguration.Panel1.Controls.Add(this.tbHeight);
            this.splConfiguration.Panel1.Controls.Add(this.cbEncoding);
            this.splConfiguration.Panel1.Controls.Add(this.tbOffset);
            this.splConfiguration.Panel1.Controls.Add(this.cbSwizzle);
            this.splConfiguration.Panel1.Controls.Add(this.heightLabel);
            this.splConfiguration.Panel1.Controls.Add(this.offsetLabel);
            this.splConfiguration.Panel1.Controls.Add(this.encLabel);
            this.splConfiguration.Panel1.Controls.Add(this.swizzleLabel);
            // 
            // splConfiguration.Panel2
            // 
            this.splConfiguration.Panel2.Controls.Add(this.splParameters);
            // 
            // btnProcess
            // 
            this.btnProcess.Location = new System.Drawing.Point(6, 138);
            this.btnProcess.Name = "btnProcess";
            this.btnProcess.Size = new System.Drawing.Size(158, 23);
            this.btnProcess.TabIndex = 12;
            this.btnProcess.Text = "Process";
            this.btnProcess.UseVisualStyleBackColor = true;
            // 
            // tbWidth
            // 
            this.tbWidth.Location = new System.Drawing.Point(64, 6);
            this.tbWidth.Name = "tbWidth";
            this.tbWidth.Size = new System.Drawing.Size(100, 20);
            this.tbWidth.TabIndex = 10;
            this.tbWidth.Text = "1";
            this.tbWidth.TextChanged += new System.EventHandler(this.tbWidth_TextChanged);
            // 
            // widthLabel
            // 
            this.widthLabel.AutoSize = true;
            this.widthLabel.Location = new System.Drawing.Point(3, 9);
            this.widthLabel.Name = "widthLabel";
            this.widthLabel.Size = new System.Drawing.Size(38, 13);
            this.widthLabel.TabIndex = 7;
            this.widthLabel.Text = "Width:";
            // 
            // tbHeight
            // 
            this.tbHeight.Location = new System.Drawing.Point(64, 32);
            this.tbHeight.Name = "tbHeight";
            this.tbHeight.Size = new System.Drawing.Size(100, 20);
            this.tbHeight.TabIndex = 11;
            this.tbHeight.Text = "1";
            this.tbHeight.TextChanged += new System.EventHandler(this.tbHeight_TextChanged);
            // 
            // cbEncoding
            // 
            this.cbEncoding.FormattingEnabled = true;
            this.cbEncoding.Location = new System.Drawing.Point(64, 84);
            this.cbEncoding.Name = "cbEncoding";
            this.cbEncoding.Size = new System.Drawing.Size(100, 21);
            this.cbEncoding.TabIndex = 0;
            this.cbEncoding.SelectedIndexChanged += new System.EventHandler(this.CbEncoding_SelectedIndexChanged);
            // 
            // tbOffset
            // 
            this.tbOffset.Location = new System.Drawing.Point(64, 58);
            this.tbOffset.Name = "tbOffset";
            this.tbOffset.Size = new System.Drawing.Size(100, 20);
            this.tbOffset.TabIndex = 2;
            this.tbOffset.Text = "0";
            this.tbOffset.TextChanged += new System.EventHandler(this.tbOffset_TextChanged);
            // 
            // cbSwizzle
            // 
            this.cbSwizzle.FormattingEnabled = true;
            this.cbSwizzle.Location = new System.Drawing.Point(64, 111);
            this.cbSwizzle.Name = "cbSwizzle";
            this.cbSwizzle.Size = new System.Drawing.Size(100, 21);
            this.cbSwizzle.TabIndex = 1;
            this.cbSwizzle.SelectedIndexChanged += new System.EventHandler(this.CbSwizzle_SelectedIndexChanged);
            // 
            // heightLabel
            // 
            this.heightLabel.AutoSize = true;
            this.heightLabel.Location = new System.Drawing.Point(3, 35);
            this.heightLabel.Name = "heightLabel";
            this.heightLabel.Size = new System.Drawing.Size(41, 13);
            this.heightLabel.TabIndex = 9;
            this.heightLabel.Text = "Height:";
            // 
            // offsetLabel
            // 
            this.offsetLabel.AutoSize = true;
            this.offsetLabel.Location = new System.Drawing.Point(3, 61);
            this.offsetLabel.Name = "offsetLabel";
            this.offsetLabel.Size = new System.Drawing.Size(54, 13);
            this.offsetLabel.TabIndex = 3;
            this.offsetLabel.Text = "FileOffset:";
            // 
            // encLabel
            // 
            this.encLabel.AutoSize = true;
            this.encLabel.Location = new System.Drawing.Point(3, 87);
            this.encLabel.Name = "encLabel";
            this.encLabel.Size = new System.Drawing.Size(55, 13);
            this.encLabel.TabIndex = 4;
            this.encLabel.Text = "Encoding:";
            // 
            // swizzleLabel
            // 
            this.swizzleLabel.AutoSize = true;
            this.swizzleLabel.Location = new System.Drawing.Point(3, 114);
            this.swizzleLabel.Name = "swizzleLabel";
            this.swizzleLabel.Size = new System.Drawing.Size(45, 13);
            this.swizzleLabel.TabIndex = 5;
            this.swizzleLabel.Text = "Swizzle:";
            // 
            // splParameters
            // 
            this.splParameters.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splParameters.Location = new System.Drawing.Point(0, 0);
            this.splParameters.Name = "splParameters";
            this.splParameters.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splParameters.Panel1
            // 
            this.splParameters.Panel1.Controls.Add(this.gbEncParameters);
            // 
            // splParameters.Panel2
            // 
            this.splParameters.Panel2.Controls.Add(this.gbSwizzleParameters);
            this.splParameters.Size = new System.Drawing.Size(429, 167);
            this.splParameters.SplitterDistance = 83;
            this.splParameters.TabIndex = 0;
            // 
            // gbEncParameters
            // 
            this.gbEncParameters.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbEncParameters.Location = new System.Drawing.Point(0, 0);
            this.gbEncParameters.Name = "gbEncParameters";
            this.gbEncParameters.Size = new System.Drawing.Size(429, 83);
            this.gbEncParameters.TabIndex = 0;
            this.gbEncParameters.TabStop = false;
            this.gbEncParameters.Text = "Encoding Parameters";
            // 
            // gbSwizzleParameters
            // 
            this.gbSwizzleParameters.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbSwizzleParameters.Location = new System.Drawing.Point(0, 0);
            this.gbSwizzleParameters.Name = "gbSwizzleParameters";
            this.gbSwizzleParameters.Size = new System.Drawing.Size(429, 80);
            this.gbSwizzleParameters.TabIndex = 0;
            this.gbSwizzleParameters.TabStop = false;
            this.gbSwizzleParameters.Text = "Swizzle Parameters";
            // 
            // RawImageViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(604, 626);
            this.Controls.Add(this.splMain);
            this.Controls.Add(this.mnuMain);
            this.MainMenuStrip = this.mnuMain;
            this.Name = "RawImageViewer";
            this.Text = "Raw Image Viewer";
            this.mnuMain.ResumeLayout(false);
            this.mnuMain.PerformLayout();
            this.splMain.Panel1.ResumeLayout(false);
            this.splMain.Panel1.PerformLayout();
            this.splMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splMain)).EndInit();
            this.splMain.ResumeLayout(false);
            this.tslPbProperties.ResumeLayout(false);
            this.tslPbProperties.PerformLayout();
            this.splConfiguration.Panel1.ResumeLayout(false);
            this.splConfiguration.Panel1.PerformLayout();
            this.splConfiguration.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splConfiguration)).EndInit();
            this.splConfiguration.ResumeLayout(false);
            this.splParameters.Panel1.ResumeLayout(false);
            this.splParameters.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splParameters)).EndInit();
            this.splParameters.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip mnuMain;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem closeToolStripMenuItem;

        private System.Windows.Forms.SplitContainer splMain;
        private System.Windows.Forms.SplitContainer splConfiguration;
        private System.Windows.Forms.SplitContainer splParameters;

        private Cyotek.Windows.Forms.ImageBox pbMain;

        private System.Windows.Forms.Label heightLabel;
        private System.Windows.Forms.Label widthLabel;
        private System.Windows.Forms.Label offsetLabel;
        private System.Windows.Forms.Label encLabel;
        private System.Windows.Forms.Label swizzleLabel;

        private System.Windows.Forms.TextBox tbWidth;
        private System.Windows.Forms.TextBox tbHeight;
        private System.Windows.Forms.TextBox tbOffset;
        private System.Windows.Forms.ComboBox cbEncoding;
        private System.Windows.Forms.ComboBox cbSwizzle;

        private System.Windows.Forms.ToolStrip tslPbProperties;
        private System.Windows.Forms.ToolStripLabel tslWidthLabel;
        private System.Windows.Forms.ToolStripLabel tslWidth;
        private System.Windows.Forms.ToolStripLabel tslHeightLabel;
        private System.Windows.Forms.ToolStripLabel tslHeight;
        private System.Windows.Forms.ToolStripLabel tslZoom;
        private System.Windows.Forms.Button btnProcess;

        private System.Windows.Forms.GroupBox gbEncParameters;
        private System.Windows.Forms.GroupBox gbSwizzleParameters;
    }
}