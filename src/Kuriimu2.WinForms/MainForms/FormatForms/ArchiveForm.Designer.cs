namespace Kuriimu2.WinForms.MainForms.FormatForms
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
            this.tlsMain = new System.Windows.Forms.ToolStrip();
            this.tsbSave = new System.Windows.Forms.ToolStripButton();
            this.tsbSaveAs = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbFind = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbProperties = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.pnlMain = new System.Windows.Forms.Panel();
            this.splMain = new System.Windows.Forms.SplitContainer();
            this.treDirectories = new System.Windows.Forms.TreeView();
            this.mnuDirectories = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.extractDirectoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.replaceDirectoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addDirectoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteDirectoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.imlFiles = new System.Windows.Forms.ImageList(this.components);
            this.lstFiles = new System.Windows.Forms.ListView();
            this.clmName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.clmSize = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.clmState = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.mnuFiles = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.extractFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.replaceFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.renameFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.openWithPluginToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.imlFilesLarge = new System.Windows.Forms.ImageList(this.components);
            this.tlsFileDetails = new System.Windows.Forms.ToolStrip();
            this.tslFileCount = new System.Windows.Forms.ToolStripLabel();
            this.tlsPreview = new System.Windows.Forms.ToolStrip();
            this.tsbFileExtract = new System.Windows.Forms.ToolStripButton();
            this.tsbFileReplace = new System.Windows.Forms.ToolStripButton();
            this.tsbFileRename = new System.Windows.Forms.ToolStripButton();
            this.tsbFileDelete = new System.Windows.Forms.ToolStripButton();
            this.tsbFileProperties = new System.Windows.Forms.ToolStripButton();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.btnSearchDelete = new System.Windows.Forms.Button();
            this.tlsMain.SuspendLayout();
            this.pnlMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splMain)).BeginInit();
            this.splMain.Panel1.SuspendLayout();
            this.splMain.Panel2.SuspendLayout();
            this.splMain.SuspendLayout();
            this.mnuDirectories.SuspendLayout();
            this.mnuFiles.SuspendLayout();
            this.tlsFileDetails.SuspendLayout();
            this.tlsPreview.SuspendLayout();
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
            this.tsbFind,
            this.toolStripSeparator2,
            this.tsbProperties,
            this.toolStripSeparator3});
            this.tlsMain.Location = new System.Drawing.Point(1, 0);
            this.tlsMain.Name = "tlsMain";
            this.tlsMain.Padding = new System.Windows.Forms.Padding(0);
            this.tlsMain.Size = new System.Drawing.Size(787, 25);
            this.tlsMain.TabIndex = 0;
            // 
            // tsbSave
            // 
            this.tsbSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbSave.Enabled = false;
            this.tsbSave.Image = global::Kuriimu2.WinForms.Properties.Resources.menu_save;
            this.tsbSave.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbSave.Name = "tsbSave";
            this.tsbSave.Size = new System.Drawing.Size(23, 22);
            this.tsbSave.Text = "Save";
            this.tsbSave.Click += new System.EventHandler(this.tsbSave_Click);
            // 
            // tsbSaveAs
            // 
            this.tsbSaveAs.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbSaveAs.Enabled = false;
            this.tsbSaveAs.Image = global::Kuriimu2.WinForms.Properties.Resources.menu_save_as;
            this.tsbSaveAs.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbSaveAs.Name = "tsbSaveAs";
            this.tsbSaveAs.Size = new System.Drawing.Size(23, 22);
            this.tsbSaveAs.Text = "Save As...";
            this.tsbSaveAs.Click += new System.EventHandler(this.tsbSaveAs_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // tsbFind
            // 
            this.tsbFind.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbFind.Enabled = false;
            this.tsbFind.Image = global::Kuriimu2.WinForms.Properties.Resources.menu_find;
            this.tsbFind.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbFind.Name = "tsbFind";
            this.tsbFind.Size = new System.Drawing.Size(23, 22);
            this.tsbFind.Text = "Find";
            this.tsbFind.Click += new System.EventHandler(this.tsbFind_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // tsbProperties
            // 
            this.tsbProperties.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbProperties.Enabled = false;
            this.tsbProperties.Image = global::Kuriimu2.WinForms.Properties.Resources.menu_properties;
            this.tsbProperties.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbProperties.Name = "tsbProperties";
            this.tsbProperties.Size = new System.Drawing.Size(23, 22);
            this.tsbProperties.Text = "Properties";
            this.tsbProperties.Click += new System.EventHandler(this.tsbProperties_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            // 
            // pnlMain
            // 
            this.pnlMain.Controls.Add(this.splMain);
            this.pnlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMain.Location = new System.Drawing.Point(1, 25);
            this.pnlMain.Name = "pnlMain";
            this.pnlMain.Size = new System.Drawing.Size(787, 445);
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
            this.splMain.Panel1.Controls.Add(this.btnSearchDelete);
            this.splMain.Panel1.Controls.Add(this.txtSearch);
            this.splMain.Panel1.Controls.Add(this.treDirectories);
            // 
            // splMain.Panel2
            // 
            this.splMain.Panel2.Controls.Add(this.lstFiles);
            this.splMain.Panel2.Controls.Add(this.tlsFileDetails);
            this.splMain.Panel2.Controls.Add(this.tlsPreview);
            this.splMain.Size = new System.Drawing.Size(787, 445);
            this.splMain.SplitterDistance = 258;
            this.splMain.TabIndex = 0;
            // 
            // treDirectories
            // 
            this.treDirectories.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.treDirectories.ContextMenuStrip = this.mnuDirectories;
            this.treDirectories.FullRowSelect = true;
            this.treDirectories.HideSelection = false;
            this.treDirectories.HotTracking = true;
            this.treDirectories.ImageIndex = 0;
            this.treDirectories.ImageList = this.imlFiles;
            this.treDirectories.Location = new System.Drawing.Point(0, 25);
            this.treDirectories.Name = "treDirectories";
            this.treDirectories.SelectedImageIndex = 0;
            this.treDirectories.ShowLines = false;
            this.treDirectories.Size = new System.Drawing.Size(258, 420);
            this.treDirectories.TabIndex = 1;
            this.treDirectories.AfterCollapse += new System.Windows.Forms.TreeViewEventHandler(this.treDirectories_AfterCollapse);
            this.treDirectories.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(this.treDirectories_AfterExpand);
            this.treDirectories.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treDirectories_AfterSelect);
            // 
            // mnuDirectories
            // 
            this.mnuDirectories.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.extractDirectoryToolStripMenuItem,
            this.replaceDirectoryToolStripMenuItem,
            this.addDirectoryToolStripMenuItem,
            this.deleteDirectoryToolStripMenuItem});
            this.mnuDirectories.Name = "mnuDirectories";
            this.mnuDirectories.Size = new System.Drawing.Size(125, 92);
            this.mnuDirectories.Opening += new System.ComponentModel.CancelEventHandler(this.mnuDirectories_Opening);
            // 
            // extractDirectoryToolStripMenuItem
            // 
            this.extractDirectoryToolStripMenuItem.Image = global::Kuriimu2.WinForms.Properties.Resources.menu_export;
            this.extractDirectoryToolStripMenuItem.Name = "extractDirectoryToolStripMenuItem";
            this.extractDirectoryToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.extractDirectoryToolStripMenuItem.Text = "E&xtract...";
            this.extractDirectoryToolStripMenuItem.Click += new System.EventHandler(this.extractDirectoryToolStripMenuItem_Click);
            // 
            // replaceDirectoryToolStripMenuItem
            // 
            this.replaceDirectoryToolStripMenuItem.Image = global::Kuriimu2.WinForms.Properties.Resources.menu_import;
            this.replaceDirectoryToolStripMenuItem.Name = "replaceDirectoryToolStripMenuItem";
            this.replaceDirectoryToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.replaceDirectoryToolStripMenuItem.Text = "&Replace...";
            this.replaceDirectoryToolStripMenuItem.Click += new System.EventHandler(this.replaceDirectoryToolStripMenuItem_Click);
            // 
            // addDirectoryToolStripMenuItem
            // 
            this.addDirectoryToolStripMenuItem.Image = global::Kuriimu2.WinForms.Properties.Resources.menu_add;
            this.addDirectoryToolStripMenuItem.Name = "addDirectoryToolStripMenuItem";
            this.addDirectoryToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.addDirectoryToolStripMenuItem.Text = "&Add...";
            this.addDirectoryToolStripMenuItem.Click += new System.EventHandler(this.addDirectoryToolStripMenuItem_Click);
            // 
            // deleteDirectoryToolStripMenuItem
            // 
            this.deleteDirectoryToolStripMenuItem.Image = global::Kuriimu2.WinForms.Properties.Resources.menu_delete;
            this.deleteDirectoryToolStripMenuItem.Name = "deleteDirectoryToolStripMenuItem";
            this.deleteDirectoryToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.deleteDirectoryToolStripMenuItem.Text = "Delete";
            this.deleteDirectoryToolStripMenuItem.Click += new System.EventHandler(this.deleteDirectoryToolStripMenuItem_Click);
            // 
            // imlFiles
            // 
            this.imlFiles.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this.imlFiles.ImageSize = new System.Drawing.Size(16, 16);
            this.imlFiles.TransparentColor = System.Drawing.Color.Transparent;
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
            this.lstFiles.HideSelection = false;
            this.lstFiles.LargeImageList = this.imlFilesLarge;
            this.lstFiles.Location = new System.Drawing.Point(0, 25);
            this.lstFiles.MultiSelect = false;
            this.lstFiles.Name = "lstFiles";
            this.lstFiles.ShowGroups = false;
            this.lstFiles.ShowItemToolTips = true;
            this.lstFiles.Size = new System.Drawing.Size(525, 395);
            this.lstFiles.SmallImageList = this.imlFiles;
            this.lstFiles.TabIndex = 3;
            this.lstFiles.UseCompatibleStateImageBehavior = false;
            this.lstFiles.View = System.Windows.Forms.View.Details;
            this.lstFiles.DoubleClick += new System.EventHandler(this.lstFiles_DoubleClick);
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
            this.mnuFiles.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.extractFileToolStripMenuItem,
            this.replaceFileToolStripMenuItem,
            this.renameFileToolStripMenuItem,
            this.deleteFileToolStripMenuItem,
            this.toolStripSeparator4,
            this.openWithPluginToolStripMenuItem});
            this.mnuFiles.Name = "mnuFiles";
            this.mnuFiles.Size = new System.Drawing.Size(167, 120);
            this.mnuFiles.Opening += new System.ComponentModel.CancelEventHandler(this.mnuFiles_Opening);
            // 
            // extractFileToolStripMenuItem
            // 
            this.extractFileToolStripMenuItem.Image = global::Kuriimu2.WinForms.Properties.Resources.menu_export;
            this.extractFileToolStripMenuItem.Name = "extractFileToolStripMenuItem";
            this.extractFileToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
            this.extractFileToolStripMenuItem.Text = "E&xtract...";
            this.extractFileToolStripMenuItem.Click += new System.EventHandler(this.extractFileToolStripMenuItem_Click);
            // 
            // replaceFileToolStripMenuItem
            // 
            this.replaceFileToolStripMenuItem.Image = global::Kuriimu2.WinForms.Properties.Resources.menu_import;
            this.replaceFileToolStripMenuItem.Name = "replaceFileToolStripMenuItem";
            this.replaceFileToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
            this.replaceFileToolStripMenuItem.Text = "&Replace...";
            this.replaceFileToolStripMenuItem.Click += new System.EventHandler(this.replaceFileToolStripMenuItem_Click);
            // 
            // renameFileToolStripMenuItem
            // 
            this.renameFileToolStripMenuItem.Image = global::Kuriimu2.WinForms.Properties.Resources.menu_field_properties;
            this.renameFileToolStripMenuItem.Name = "renameFileToolStripMenuItem";
            this.renameFileToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
            this.renameFileToolStripMenuItem.Text = "Re&name...";
            this.renameFileToolStripMenuItem.Click += new System.EventHandler(this.renameFileToolStripMenuItem_Click);
            // 
            // deleteFileToolStripMenuItem
            // 
            this.deleteFileToolStripMenuItem.Image = global::Kuriimu2.WinForms.Properties.Resources.menu_delete;
            this.deleteFileToolStripMenuItem.Name = "deleteFileToolStripMenuItem";
            this.deleteFileToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
            this.deleteFileToolStripMenuItem.Text = "&Delete";
            this.deleteFileToolStripMenuItem.Click += new System.EventHandler(this.deleteFileToolStripMenuItem_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(163, 6);
            // 
            // openWithPluginToolStripMenuItem
            // 
            this.openWithPluginToolStripMenuItem.Name = "openWithPluginToolStripMenuItem";
            this.openWithPluginToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
            this.openWithPluginToolStripMenuItem.Text = "Open with plugin";
            // 
            // imlFilesLarge
            // 
            this.imlFilesLarge.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this.imlFilesLarge.ImageSize = new System.Drawing.Size(16, 16);
            this.imlFilesLarge.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // tlsFileDetails
            // 
            this.tlsFileDetails.AutoSize = false;
            this.tlsFileDetails.BackColor = System.Drawing.Color.Transparent;
            this.tlsFileDetails.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tlsFileDetails.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tlsFileDetails.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tslFileCount});
            this.tlsFileDetails.Location = new System.Drawing.Point(0, 420);
            this.tlsFileDetails.Name = "tlsFileDetails";
            this.tlsFileDetails.Size = new System.Drawing.Size(525, 25);
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
            this.tsbFileExtract,
            this.tsbFileReplace,
            this.tsbFileRename,
            this.tsbFileDelete,
            this.tsbFileProperties});
            this.tlsPreview.Location = new System.Drawing.Point(0, 0);
            this.tlsPreview.Name = "tlsPreview";
            this.tlsPreview.Size = new System.Drawing.Size(525, 25);
            this.tlsPreview.TabIndex = 0;
            // 
            // tsbFileExtract
            // 
            this.tsbFileExtract.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbFileExtract.Enabled = false;
            this.tsbFileExtract.Image = global::Kuriimu2.WinForms.Properties.Resources.menu_export;
            this.tsbFileExtract.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbFileExtract.Name = "tsbFileExtract";
            this.tsbFileExtract.Size = new System.Drawing.Size(23, 22);
            this.tsbFileExtract.Text = "Extract File";
            this.tsbFileExtract.Click += new System.EventHandler(this.tsbFileExtract_Click);
            // 
            // tsbFileReplace
            // 
            this.tsbFileReplace.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbFileReplace.Enabled = false;
            this.tsbFileReplace.Image = global::Kuriimu2.WinForms.Properties.Resources.menu_import;
            this.tsbFileReplace.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbFileReplace.Name = "tsbFileReplace";
            this.tsbFileReplace.Size = new System.Drawing.Size(23, 22);
            this.tsbFileReplace.Text = "Replace File";
            this.tsbFileReplace.Click += new System.EventHandler(this.tsbFileReplace_Click);
            // 
            // tsbFileRename
            // 
            this.tsbFileRename.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbFileRename.Enabled = false;
            this.tsbFileRename.Image = global::Kuriimu2.WinForms.Properties.Resources.menu_field_properties;
            this.tsbFileRename.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbFileRename.Name = "tsbFileRename";
            this.tsbFileRename.Size = new System.Drawing.Size(23, 22);
            this.tsbFileRename.Text = "Rename File";
            this.tsbFileRename.Click += new System.EventHandler(this.tsbFileRename_Click);
            // 
            // tsbFileDelete
            // 
            this.tsbFileDelete.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbFileDelete.Enabled = false;
            this.tsbFileDelete.Image = global::Kuriimu2.WinForms.Properties.Resources.menu_delete;
            this.tsbFileDelete.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbFileDelete.Name = "tsbFileDelete";
            this.tsbFileDelete.Size = new System.Drawing.Size(23, 22);
            this.tsbFileDelete.Text = "Delete File";
            this.tsbFileDelete.Click += new System.EventHandler(this.tsbFileDelete_Click);
            // 
            // tsbFileProperties
            // 
            this.tsbFileProperties.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbFileProperties.Enabled = false;
            this.tsbFileProperties.Image = global::Kuriimu2.WinForms.Properties.Resources.menu_properties;
            this.tsbFileProperties.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbFileProperties.Name = "tsbFileProperties";
            this.tsbFileProperties.Size = new System.Drawing.Size(23, 22);
            this.tsbFileProperties.Text = "File Properties";
            this.tsbFileProperties.Click += new System.EventHandler(this.tsbFileProperties_Click);
            // 
            // txtSearch
            // 
            this.txtSearch.ForeColor = System.Drawing.SystemColors.ScrollBar;
            this.txtSearch.Location = new System.Drawing.Point(3, 3);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(229, 20);
            this.txtSearch.TabIndex = 2;
            this.txtSearch.Text = "Search archive...";
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            this.txtSearch.Enter += new System.EventHandler(this.txtSearch_Enter);
            this.txtSearch.Leave += new System.EventHandler(this.txtSearch_Leave);
            // 
            // btnSearchDelete
            // 
            this.btnSearchDelete.Location = new System.Drawing.Point(238, 3);
            this.btnSearchDelete.Name = "btnSearchDelete";
            this.btnSearchDelete.Size = new System.Drawing.Size(17, 20);
            this.btnSearchDelete.TabIndex = 3;
            this.btnSearchDelete.Text = "X";
            this.btnSearchDelete.UseVisualStyleBackColor = true;
            this.btnSearchDelete.Click += new System.EventHandler(this.btnSearchDelete_Click);
            // 
            // ArchiveForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.Controls.Add(this.pnlMain);
            this.Controls.Add(this.tlsMain);
            this.Name = "ArchiveForm";
            this.Padding = new System.Windows.Forms.Padding(1, 0, 1, 1);
            this.Size = new System.Drawing.Size(789, 471);
            this.tlsMain.ResumeLayout(false);
            this.tlsMain.PerformLayout();
            this.pnlMain.ResumeLayout(false);
            this.splMain.Panel1.ResumeLayout(false);
            this.splMain.Panel1.PerformLayout();
            this.splMain.Panel2.ResumeLayout(false);
            this.splMain.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splMain)).EndInit();
            this.splMain.ResumeLayout(false);
            this.mnuDirectories.ResumeLayout(false);
            this.mnuFiles.ResumeLayout(false);
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
        private System.Windows.Forms.TreeView treDirectories;
        private System.Windows.Forms.ToolStrip tlsPreview;
        private System.Windows.Forms.ContextMenuStrip mnuFiles;
        private System.Windows.Forms.ContextMenuStrip mnuDirectories;
        private System.Windows.Forms.ToolStrip tlsFileDetails;
        private System.Windows.Forms.ListView lstFiles;
        private System.Windows.Forms.ToolStripButton tsbSave;
        private System.Windows.Forms.ToolStripButton tsbSaveAs;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton tsbFind;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton tsbProperties;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ColumnHeader clmName;
        private System.Windows.Forms.ColumnHeader clmSize;
        private System.Windows.Forms.ColumnHeader clmState;
        private System.Windows.Forms.ToolStripLabel tslFileCount;
        private System.Windows.Forms.ToolStripButton tsbFileExtract;
        private System.Windows.Forms.ToolStripButton tsbFileReplace;
        private System.Windows.Forms.ToolStripButton tsbFileRename;
        private System.Windows.Forms.ToolStripButton tsbFileDelete;
        private System.Windows.Forms.ToolStripButton tsbFileProperties;
        private System.Windows.Forms.ImageList imlFiles;
        private System.Windows.Forms.ImageList imlFilesLarge;
        private System.Windows.Forms.ToolStripMenuItem extractDirectoryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem replaceDirectoryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addDirectoryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteDirectoryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem extractFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem replaceFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem renameFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripMenuItem openWithPluginToolStripMenuItem;
        private System.Windows.Forms.Button btnSearchDelete;
        private System.Windows.Forms.TextBox txtSearch;
    }
}
