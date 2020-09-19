using System.Drawing;
using System.Windows.Forms;

namespace Kuriimu2.WinForms.BatchForms
{
    partial class BaseBatchForm
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
            this.splMain = new System.Windows.Forms.SplitContainer();
            this.cmbPlugins = new System.Windows.Forms.ComboBox();
            this.lblPlugins = new System.Windows.Forms.Label();
            this.txtSourcePath = new System.Windows.Forms.TextBox();
            this.txtDestinationPath = new System.Windows.Forms.TextBox();
            this.btnSourceFolder = new System.Windows.Forms.Button();
            this.btnDestinationFolder = new System.Windows.Forms.Button();
            this.btnExecute = new System.Windows.Forms.Button();
            this.chkSubDirectories = new System.Windows.Forms.CheckBox();
            this.txtLog = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            //
            // splMain
            //
            this.splMain.Name = "splMain";
            this.splMain.Dock = DockStyle.Fill;
            this.splMain.SplitterDistance = 60;
            this.splMain.IsSplitterFixed = true;
            //
            // splMain.Panel1
            //
            this.splMain.Panel1.Controls.Add(this.cmbPlugins);
            this.splMain.Panel1.Controls.Add(this.lblPlugins);
            this.splMain.Panel1.Controls.Add(this.txtSourcePath);
            this.splMain.Panel1.Controls.Add(this.txtDestinationPath);
            this.splMain.Panel1.Controls.Add(this.btnSourceFolder);
            this.splMain.Panel1.Controls.Add(this.btnDestinationFolder);
            this.splMain.Panel1.Controls.Add(this.chkSubDirectories);
            this.splMain.Panel1.Controls.Add(this.btnExecute);
            //
            // splMain.Panel2
            //
            this.splMain.Panel2.Controls.Add(this.txtLog);
            // 
            // cmbPlugins
            // 
            this.cmbPlugins.FormattingEnabled = true;
            this.cmbPlugins.Location = new System.Drawing.Point(12, 29);
            this.cmbPlugins.Name = "cmbPlugins";
            this.cmbPlugins.Size = new System.Drawing.Size(212, 21);
            this.cmbPlugins.TabIndex = 0;
            // 
            // lblPlugins
            // 
            this.lblPlugins.AutoSize = true;
            this.lblPlugins.Location = new System.Drawing.Point(13, 13);
            this.lblPlugins.Name = "lblPlugins";
            this.lblPlugins.Size = new System.Drawing.Size(85, 13);
            this.lblPlugins.TabIndex = 1;
            this.lblPlugins.Text = "Plugins:";
            // 
            // txtSourcePath
            // 
            this.txtSourcePath.Location = new System.Drawing.Point(12, 57);
            this.txtSourcePath.Name = "txtSourcePath";
            this.txtSourcePath.ReadOnly = true;
            this.txtSourcePath.Size = new System.Drawing.Size(212, 20);
            this.txtSourcePath.TabIndex = 3;
            // 
            // btnSourceFolder
            // 
            this.btnSourceFolder.Location = new System.Drawing.Point(12, 83);
            this.btnSourceFolder.Name = "btnSourceFolder";
            this.btnSourceFolder.Size = new System.Drawing.Size(108, 23);
            this.btnSourceFolder.TabIndex = 4;
            this.btnSourceFolder.Text = "Select Folder...";
            this.btnSourceFolder.UseVisualStyleBackColor = true;
            this.btnSourceFolder.Click += new System.EventHandler(this.btnSourceFolder_Click);
            //
            // txtDestinationPath
            //
            this.txtDestinationPath.Location = new Point(12, 112);
            this.txtDestinationPath.Name = "txtDestinationPath";
            this.txtDestinationPath.ReadOnly = true;
            this.txtDestinationPath.Size = new Size(212, 20);
            //
            // btnDestinationFolder
            //
            this.btnDestinationFolder.Location = new Point(12, 138);
            this.btnDestinationFolder.Name = "btnDestinationFolder";
            this.btnDestinationFolder.Size = new Size(212, 23);
            this.btnDestinationFolder.Text = "Select Folder...";
            this.btnDestinationFolder.Click += btnDestinationFolder_Click;
            // 
            // btnExecute
            // 
            //this.btnExecute.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnExecute.Location = new System.Drawing.Point(12, 186);
            this.btnExecute.Name = "btnExecute";
            this.btnExecute.Size = new System.Drawing.Size(212, 23);
            this.btnExecute.TabIndex = 6;
            this.btnExecute.Text = "Execute";
            this.btnExecute.UseVisualStyleBackColor = true;
            this.btnExecute.Click += new System.EventHandler(this.btnExecute_Click);
            // 
            // chkSubDirectories
            // 
            this.chkSubDirectories.AutoSize = true;
            this.chkSubDirectories.Checked = true;
            this.chkSubDirectories.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkSubDirectories.Location = new System.Drawing.Point(126, 87);
            this.chkSubDirectories.Name = "chkSubDirectories";
            this.chkSubDirectories.Size = new System.Drawing.Size(98, 17);
            this.chkSubDirectories.TabIndex = 7;
            this.chkSubDirectories.Text = "Sub Directories";
            this.chkSubDirectories.UseVisualStyleBackColor = true;
            // 
            // txtLog
            // 
            this.txtLog.Dock = DockStyle.Fill;
            this.txtLog.BackColor = System.Drawing.Color.Black;
            this.txtLog.ForeColor = Color.FromArgb(0x10, 0xC2, 0x0E);
            this.txtLog.Location = new System.Drawing.Point(230, 142);
            this.txtLog.Name = "txtLog";
            this.txtLog.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(341, 67);
            this.txtLog.TabIndex = 9;
            this.txtLog.Text = "";
            // 
            // BaseBatchForm
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(583, 221);
            this.Controls.Add(this.splMain);
            this.Name = "BaseBatchForm";
            this.Text = "Batch";
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.BatchForm_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.BatchForm_DragEnter);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer splMain;
        private System.Windows.Forms.Label lblPlugins;
        private System.Windows.Forms.ComboBox cmbPlugins;
        private System.Windows.Forms.TextBox txtSourcePath;
        private System.Windows.Forms.TextBox txtDestinationPath;
        private System.Windows.Forms.Button btnSourceFolder;
        private System.Windows.Forms.Button btnDestinationFolder;
        private System.Windows.Forms.Button btnExecute;
        private System.Windows.Forms.CheckBox chkSubDirectories;
        private System.Windows.Forms.RichTextBox txtLog;
    }
}