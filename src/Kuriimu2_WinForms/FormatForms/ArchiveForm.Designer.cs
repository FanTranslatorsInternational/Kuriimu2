namespace Kuriimu2_WinForms.FormatForms
{
    partial class ArchiveForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ArchiveForm));
            this.tlsMain = new System.Windows.Forms.ToolStrip();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton2 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton3 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton4 = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButton5 = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButton6 = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButton7 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton8 = new System.Windows.Forms.ToolStripButton();
            this.pnlMain = new System.Windows.Forms.Panel();
            this.splMain = new System.Windows.Forms.SplitContainer();
            this.treDirectories = new System.Windows.Forms.TreeView();
            this.mnuDirectories = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tlsFiles = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            this.lstFiles = new System.Windows.Forms.ListView();
            this.clmName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.clmSize = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.clmState = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.mnuFiles = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tlsFileDetails = new System.Windows.Forms.ToolStrip();
            this.tslFileCount = new System.Windows.Forms.ToolStripLabel();
            this.tlsPreview = new System.Windows.Forms.ToolStrip();
            this.toolStripButton9 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton10 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton11 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton12 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton13 = new System.Windows.Forms.ToolStripButton();
            this.tlsMain.SuspendLayout();
            this.pnlMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splMain)).BeginInit();
            this.splMain.Panel1.SuspendLayout();
            this.splMain.Panel2.SuspendLayout();
            this.splMain.SuspendLayout();
            this.tlsFiles.SuspendLayout();
            this.tlsFileDetails.SuspendLayout();
            this.tlsPreview.SuspendLayout();
            this.SuspendLayout();
            // 
            // tlsMain
            // 
            this.tlsMain.BackColor = System.Drawing.Color.Transparent;
            this.tlsMain.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tlsMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton1,
            this.toolStripButton2,
            this.toolStripButton3,
            this.toolStripButton4,
            this.toolStripSeparator1,
            this.toolStripButton5,
            this.toolStripSeparator2,
            this.toolStripButton6,
            this.toolStripSeparator3,
            this.toolStripButton7,
            this.toolStripButton8});
            this.tlsMain.Location = new System.Drawing.Point(0, 0);
            this.tlsMain.Name = "tlsMain";
            this.tlsMain.Size = new System.Drawing.Size(789, 25);
            this.tlsMain.TabIndex = 0;
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton1.Text = "toolStripButton1";
            // 
            // toolStripButton2
            // 
            this.toolStripButton2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton2.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton2.Image")));
            this.toolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton2.Name = "toolStripButton2";
            this.toolStripButton2.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton2.Text = "toolStripButton2";
            // 
            // toolStripButton3
            // 
            this.toolStripButton3.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton3.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton3.Image")));
            this.toolStripButton3.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton3.Name = "toolStripButton3";
            this.toolStripButton3.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton3.Text = "toolStripButton3";
            // 
            // toolStripButton4
            // 
            this.toolStripButton4.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton4.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton4.Image")));
            this.toolStripButton4.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton4.Name = "toolStripButton4";
            this.toolStripButton4.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton4.Text = "toolStripButton4";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButton5
            // 
            this.toolStripButton5.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton5.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton5.Image")));
            this.toolStripButton5.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton5.Name = "toolStripButton5";
            this.toolStripButton5.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton5.Text = "toolStripButton5";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButton6
            // 
            this.toolStripButton6.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton6.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton6.Image")));
            this.toolStripButton6.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton6.Name = "toolStripButton6";
            this.toolStripButton6.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton6.Text = "toolStripButton6";
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButton7
            // 
            this.toolStripButton7.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton7.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton7.Image")));
            this.toolStripButton7.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton7.Name = "toolStripButton7";
            this.toolStripButton7.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton7.Text = "toolStripButton7";
            // 
            // toolStripButton8
            // 
            this.toolStripButton8.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton8.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton8.Image")));
            this.toolStripButton8.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton8.Name = "toolStripButton8";
            this.toolStripButton8.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton8.Text = "toolStripButton8";
            // 
            // pnlMain
            // 
            this.pnlMain.Controls.Add(this.splMain);
            this.pnlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMain.Location = new System.Drawing.Point(0, 25);
            this.pnlMain.Name = "pnlMain";
            this.pnlMain.Size = new System.Drawing.Size(789, 446);
            this.pnlMain.TabIndex = 1;
            // 
            // splMain
            // 
            this.splMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splMain.Location = new System.Drawing.Point(0, 0);
            this.splMain.Name = "splMain";
            // 
            // splMain.Panel1
            // 
            this.splMain.Panel1.Controls.Add(this.treDirectories);
            this.splMain.Panel1.Controls.Add(this.tlsFiles);
            // 
            // splMain.Panel2
            // 
            this.splMain.Panel2.Controls.Add(this.lstFiles);
            this.splMain.Panel2.Controls.Add(this.tlsFileDetails);
            this.splMain.Panel2.Controls.Add(this.tlsPreview);
            this.splMain.Size = new System.Drawing.Size(789, 446);
            this.splMain.SplitterDistance = 262;
            this.splMain.TabIndex = 0;
            // 
            // treDirectories
            // 
            this.treDirectories.AllowDrop = true;
            this.treDirectories.ContextMenuStrip = this.mnuDirectories;
            this.treDirectories.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treDirectories.FullRowSelect = true;
            this.treDirectories.HideSelection = false;
            this.treDirectories.HotTracking = true;
            this.treDirectories.Location = new System.Drawing.Point(0, 25);
            this.treDirectories.Name = "treDirectories";
            this.treDirectories.ShowLines = false;
            this.treDirectories.Size = new System.Drawing.Size(262, 421);
            this.treDirectories.TabIndex = 1;
            this.treDirectories.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treDirectories_AfterSelect);
            // 
            // mnuDirectories
            // 
            this.mnuDirectories.Name = "mnuDirectories";
            this.mnuDirectories.Size = new System.Drawing.Size(61, 4);
            // 
            // tlsFiles
            // 
            this.tlsFiles.BackColor = System.Drawing.Color.Transparent;
            this.tlsFiles.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tlsFiles.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel2});
            this.tlsFiles.Location = new System.Drawing.Point(0, 0);
            this.tlsFiles.Name = "tlsFiles";
            this.tlsFiles.Size = new System.Drawing.Size(262, 25);
            this.tlsFiles.TabIndex = 0;
            // 
            // toolStripLabel2
            // 
            this.toolStripLabel2.Name = "toolStripLabel2";
            this.toolStripLabel2.Size = new System.Drawing.Size(83, 22);
            this.toolStripLabel2.Text = "Directory Tree:";
            // 
            // lstFiles
            // 
            this.lstFiles.Activation = System.Windows.Forms.ItemActivation.OneClick;
            this.lstFiles.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.clmName,
            this.clmSize,
            this.clmState});
            this.lstFiles.ContextMenuStrip = this.mnuFiles;
            this.lstFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstFiles.FullRowSelect = true;
            this.lstFiles.Location = new System.Drawing.Point(0, 25);
            this.lstFiles.MultiSelect = false;
            this.lstFiles.Name = "lstFiles";
            this.lstFiles.ShowGroups = false;
            this.lstFiles.ShowItemToolTips = true;
            this.lstFiles.Size = new System.Drawing.Size(523, 396);
            this.lstFiles.TabIndex = 3;
            this.lstFiles.UseCompatibleStateImageBehavior = false;
            this.lstFiles.View = System.Windows.Forms.View.Details;
            // 
            // clmName
            // 
            this.clmName.Text = "Name";
            this.clmName.Width = 283;
            // 
            // clmSize
            // 
            this.clmSize.Text = "Size";
            this.clmSize.Width = 81;
            // 
            // clmState
            // 
            this.clmState.Text = "State";
            this.clmState.Width = 106;
            // 
            // mnuFiles
            // 
            this.mnuFiles.Name = "mnuFiles";
            this.mnuFiles.Size = new System.Drawing.Size(61, 4);
            // 
            // tlsFileDetails
            // 
            this.tlsFileDetails.AutoSize = false;
            this.tlsFileDetails.BackColor = System.Drawing.Color.Transparent;
            this.tlsFileDetails.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tlsFileDetails.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tlsFileDetails.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tslFileCount});
            this.tlsFileDetails.Location = new System.Drawing.Point(0, 421);
            this.tlsFileDetails.Name = "tlsFileDetails";
            this.tlsFileDetails.Size = new System.Drawing.Size(523, 25);
            this.tlsFileDetails.TabIndex = 2;
            // 
            // tslFileCount
            // 
            this.tslFileCount.Name = "tslFileCount";
            this.tslFileCount.Size = new System.Drawing.Size(58, 22);
            this.tslFileCount.Text = "FileCount";
            // 
            // tlsPreview
            // 
            this.tlsPreview.BackColor = System.Drawing.Color.Transparent;
            this.tlsPreview.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tlsPreview.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton9,
            this.toolStripButton10,
            this.toolStripButton11,
            this.toolStripButton12,
            this.toolStripButton13});
            this.tlsPreview.Location = new System.Drawing.Point(0, 0);
            this.tlsPreview.Name = "tlsPreview";
            this.tlsPreview.Size = new System.Drawing.Size(523, 25);
            this.tlsPreview.TabIndex = 0;
            // 
            // toolStripButton9
            // 
            this.toolStripButton9.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton9.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton9.Image")));
            this.toolStripButton9.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton9.Name = "toolStripButton9";
            this.toolStripButton9.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton9.Text = "toolStripButton9";
            // 
            // toolStripButton10
            // 
            this.toolStripButton10.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton10.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton10.Image")));
            this.toolStripButton10.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton10.Name = "toolStripButton10";
            this.toolStripButton10.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton10.Text = "toolStripButton10";
            // 
            // toolStripButton11
            // 
            this.toolStripButton11.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton11.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton11.Image")));
            this.toolStripButton11.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton11.Name = "toolStripButton11";
            this.toolStripButton11.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton11.Text = "toolStripButton11";
            // 
            // toolStripButton12
            // 
            this.toolStripButton12.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton12.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton12.Image")));
            this.toolStripButton12.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton12.Name = "toolStripButton12";
            this.toolStripButton12.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton12.Text = "toolStripButton12";
            // 
            // toolStripButton13
            // 
            this.toolStripButton13.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton13.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton13.Image")));
            this.toolStripButton13.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton13.Name = "toolStripButton13";
            this.toolStripButton13.Size = new System.Drawing.Size(23, 22);
            this.toolStripButton13.Text = "toolStripButton13";
            // 
            // ArchiveForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pnlMain);
            this.Controls.Add(this.tlsMain);
            this.Name = "ArchiveForm";
            this.Size = new System.Drawing.Size(789, 471);
            this.Load += new System.EventHandler(this.ArchiveForm_Load);
            this.tlsMain.ResumeLayout(false);
            this.tlsMain.PerformLayout();
            this.pnlMain.ResumeLayout(false);
            this.splMain.Panel1.ResumeLayout(false);
            this.splMain.Panel1.PerformLayout();
            this.splMain.Panel2.ResumeLayout(false);
            this.splMain.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splMain)).EndInit();
            this.splMain.ResumeLayout(false);
            this.tlsFiles.ResumeLayout(false);
            this.tlsFiles.PerformLayout();
            this.tlsFileDetails.ResumeLayout(false);
            this.tlsFileDetails.PerformLayout();
            this.tlsPreview.ResumeLayout(false);
            this.tlsPreview.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip tlsMain;
        private System.Windows.Forms.Panel pnlMain;
        private System.Windows.Forms.SplitContainer splMain;
        private System.Windows.Forms.ToolStrip tlsFiles;
        private System.Windows.Forms.TreeView treDirectories;
        private System.Windows.Forms.ToolStrip tlsPreview;
        private System.Windows.Forms.ContextMenuStrip mnuFiles;
        private System.Windows.Forms.ContextMenuStrip mnuDirectories;
        private System.Windows.Forms.ToolStrip tlsFileDetails;
        private System.Windows.Forms.ListView lstFiles;
        private System.Windows.Forms.ToolStripButton toolStripButton1;
        private System.Windows.Forms.ToolStripButton toolStripButton2;
        private System.Windows.Forms.ToolStripButton toolStripButton3;
        private System.Windows.Forms.ToolStripButton toolStripButton4;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton toolStripButton5;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton toolStripButton6;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripButton toolStripButton7;
        private System.Windows.Forms.ToolStripButton toolStripButton8;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
        private System.Windows.Forms.ColumnHeader clmName;
        private System.Windows.Forms.ColumnHeader clmSize;
        private System.Windows.Forms.ColumnHeader clmState;
        private System.Windows.Forms.ToolStripLabel tslFileCount;
        private System.Windows.Forms.ToolStripButton toolStripButton9;
        private System.Windows.Forms.ToolStripButton toolStripButton10;
        private System.Windows.Forms.ToolStripButton toolStripButton11;
        private System.Windows.Forms.ToolStripButton toolStripButton12;
        private System.Windows.Forms.ToolStripButton toolStripButton13;
    }
}
