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
            this.chkSubDirectories = new System.Windows.Forms.CheckBox();
            this.btnExecute = new System.Windows.Forms.Button();
            this.splLogging = new System.Windows.Forms.SplitContainer();
            this.lblTime = new System.Windows.Forms.Label();
            this.txtLog = new System.Windows.Forms.RichTextBox();
            ((System.ComponentModel.ISupportInitialize)(this.splMain)).BeginInit();
            this.splMain.Panel1.SuspendLayout();
            this.splMain.Panel2.SuspendLayout();
            this.splMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splLogging)).BeginInit();
            this.splLogging.Panel1.SuspendLayout();
            this.splLogging.Panel2.SuspendLayout();
            this.splLogging.SuspendLayout();
            this.SuspendLayout();
            // 
            // splMain
            // 
            this.splMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splMain.IsSplitterFixed = true;
            this.splMain.Location = new System.Drawing.Point(0, 0);
            this.splMain.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.splMain.Name = "splMain";
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
            this.splMain.Panel2.Controls.Add(this.splLogging);
            this.splMain.Size = new System.Drawing.Size(680, 255);
            this.splMain.SplitterDistance = 272;
            this.splMain.SplitterWidth = 5;
            this.splMain.TabIndex = 0;
            // 
            // cmbPlugins
            // 
            this.cmbPlugins.FormattingEnabled = true;
            this.cmbPlugins.Location = new System.Drawing.Point(14, 33);
            this.cmbPlugins.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cmbPlugins.Name = "cmbPlugins";
            this.cmbPlugins.Size = new System.Drawing.Size(247, 23);
            this.cmbPlugins.TabIndex = 0;
            // 
            // lblPlugins
            // 
            this.lblPlugins.AutoSize = true;
            this.lblPlugins.Location = new System.Drawing.Point(15, 15);
            this.lblPlugins.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblPlugins.Name = "lblPlugins";
            this.lblPlugins.Size = new System.Drawing.Size(49, 15);
            this.lblPlugins.TabIndex = 1;
            this.lblPlugins.Text = "Plugins:";
            // 
            // txtSourcePath
            // 
            this.txtSourcePath.Location = new System.Drawing.Point(14, 66);
            this.txtSourcePath.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtSourcePath.Name = "txtSourcePath";
            this.txtSourcePath.ReadOnly = true;
            this.txtSourcePath.Size = new System.Drawing.Size(247, 23);
            this.txtSourcePath.TabIndex = 3;
            // 
            // txtDestinationPath
            // 
            this.txtDestinationPath.Location = new System.Drawing.Point(14, 129);
            this.txtDestinationPath.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtDestinationPath.Name = "txtDestinationPath";
            this.txtDestinationPath.ReadOnly = true;
            this.txtDestinationPath.Size = new System.Drawing.Size(247, 23);
            this.txtDestinationPath.TabIndex = 4;
            // 
            // btnSourceFolder
            // 
            this.btnSourceFolder.Location = new System.Drawing.Point(14, 96);
            this.btnSourceFolder.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnSourceFolder.Name = "btnSourceFolder";
            this.btnSourceFolder.Size = new System.Drawing.Size(126, 27);
            this.btnSourceFolder.TabIndex = 4;
            this.btnSourceFolder.Text = "Select Folder...";
            this.btnSourceFolder.UseVisualStyleBackColor = true;
            this.btnSourceFolder.Click += new System.EventHandler(this.btnSourceFolder_Click);
            // 
            // btnDestinationFolder
            // 
            this.btnDestinationFolder.Location = new System.Drawing.Point(14, 159);
            this.btnDestinationFolder.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnDestinationFolder.Name = "btnDestinationFolder";
            this.btnDestinationFolder.Size = new System.Drawing.Size(247, 27);
            this.btnDestinationFolder.TabIndex = 5;
            this.btnDestinationFolder.Text = "Select Folder...";
            this.btnDestinationFolder.Click += btnDestinationFolder_Click;
            // 
            // chkSubDirectories
            // 
            this.chkSubDirectories.AutoSize = true;
            this.chkSubDirectories.Checked = true;
            this.chkSubDirectories.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkSubDirectories.Location = new System.Drawing.Point(147, 100);
            this.chkSubDirectories.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.chkSubDirectories.Name = "chkSubDirectories";
            this.chkSubDirectories.Size = new System.Drawing.Size(105, 19);
            this.chkSubDirectories.TabIndex = 7;
            this.chkSubDirectories.Text = "Sub Directories";
            this.chkSubDirectories.UseVisualStyleBackColor = true;
            // 
            // btnExecute
            // 
            this.btnExecute.Location = new System.Drawing.Point(14, 215);
            this.btnExecute.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnExecute.Name = "btnExecute";
            this.btnExecute.Size = new System.Drawing.Size(247, 27);
            this.btnExecute.TabIndex = 6;
            this.btnExecute.Text = "Execute";
            this.btnExecute.UseVisualStyleBackColor = true;
            this.btnExecute.Click += new System.EventHandler(this.btnExecute_Click);
            // 
            // splLogging
            // 
            this.splLogging.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splLogging.Location = new System.Drawing.Point(0, 0);
            this.splLogging.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.splLogging.Name = "splLogging";
            this.splLogging.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.splLogging.IsSplitterFixed = true;
            // 
            // splLogging.Panel1
            // 
            this.splLogging.Panel1.Controls.Add(this.lblTime);
            // 
            // splLogging.Panel2
            // 
            this.splLogging.Panel2.Controls.Add(this.txtLog);
            this.splLogging.Size = new System.Drawing.Size(403, 255);
            this.splLogging.SplitterDistance = 25;
            this.splLogging.SplitterWidth = 5;
            this.splLogging.TabIndex = 0;
            // 
            // lblTime
            // 
            this.lblTime.Location = new System.Drawing.Point(0, 0);
            this.lblTime.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblTime.Name = "lblTime";
            this.lblTime.TabIndex = 0;
            this.lblTime.Text = "Avg time per file:";
            this.lblTime.AutoSize = true;
            // 
            // txtLog
            // 
            this.txtLog.BackColor = System.Drawing.Color.Black;
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(16)))), ((int)(((byte)(194)))), ((int)(((byte)(14)))));
            this.txtLog.Location = new System.Drawing.Point(0, 0);
            this.txtLog.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtLog.Name = "txtLog";
            this.txtLog.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(403, 225);
            this.txtLog.TabIndex = 9;
            this.txtLog.Text = "";
            // 
            // BaseBatchForm
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(680, 255);
            this.Controls.Add(this.splMain);
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "BaseBatchForm";
            this.Text = "Batch";
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.BatchForm_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.BatchForm_DragEnter);
            this.Closed += BaseBatchForm_Closed;
            this.splMain.Panel1.ResumeLayout(false);
            this.splMain.Panel1.PerformLayout();
            this.splMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splMain)).EndInit();
            this.splMain.ResumeLayout(false);
            this.splLogging.Panel1.ResumeLayout(false);
            this.splLogging.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splLogging)).EndInit();
            this.splLogging.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splMain;
        private System.Windows.Forms.SplitContainer splLogging;
        private System.Windows.Forms.Label lblPlugins;
        private System.Windows.Forms.ComboBox cmbPlugins;
        private System.Windows.Forms.TextBox txtSourcePath;
        private System.Windows.Forms.TextBox txtDestinationPath;
        private System.Windows.Forms.Button btnSourceFolder;
        private System.Windows.Forms.Button btnDestinationFolder;
        private System.Windows.Forms.Button btnExecute;
        private System.Windows.Forms.CheckBox chkSubDirectories;
        private System.Windows.Forms.RichTextBox txtLog;
        private System.Windows.Forms.Label lblTime;
    }
}