namespace Kuriimu2.WinForms.ExtensionForms
{
    partial class TypeExtensionForm<TExtension,TResult>
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
            this.cmbExtensions = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.gbTypeExtensionParameters = new System.Windows.Forms.GroupBox();
            this.txtPath = new System.Windows.Forms.TextBox();
            this.btnFolder = new System.Windows.Forms.Button();
            this.btnFile = new System.Windows.Forms.Button();
            this.btnExecute = new System.Windows.Forms.Button();
            this.chkSubDirectories = new System.Windows.Forms.CheckBox();
            this.chkAutoExecute = new System.Windows.Forms.CheckBox();
            this.txtLog = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // cmbExtensions
            // 
            this.cmbExtensions.FormattingEnabled = true;
            this.cmbExtensions.Location = new System.Drawing.Point(12, 29);
            this.cmbExtensions.Name = "cmbExtensions";
            this.cmbExtensions.Size = new System.Drawing.Size(212, 21);
            this.cmbExtensions.TabIndex = 0;
            this.cmbExtensions.SelectedIndexChanged += new System.EventHandler(this.cmbExtensions_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(85, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "TypeExtensions:";
            // 
            // gbTypeExtensionParameters
            // 
            this.gbTypeExtensionParameters.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbTypeExtensionParameters.Location = new System.Drawing.Point(230, 12);
            this.gbTypeExtensionParameters.Name = "gbTypeExtensionParameters";
            this.gbTypeExtensionParameters.Size = new System.Drawing.Size(341, 123);
            this.gbTypeExtensionParameters.TabIndex = 2;
            this.gbTypeExtensionParameters.TabStop = false;
            this.gbTypeExtensionParameters.Text = "Parameters";
            // 
            // txtPath
            // 
            this.txtPath.Location = new System.Drawing.Point(12, 57);
            this.txtPath.Name = "txtPath";
            this.txtPath.ReadOnly = true;
            this.txtPath.Size = new System.Drawing.Size(212, 20);
            this.txtPath.TabIndex = 3;
            // 
            // btnFolder
            // 
            this.btnFolder.Location = new System.Drawing.Point(12, 83);
            this.btnFolder.Name = "btnFolder";
            this.btnFolder.Size = new System.Drawing.Size(108, 23);
            this.btnFolder.TabIndex = 4;
            this.btnFolder.Text = "Select Folder...";
            this.btnFolder.UseVisualStyleBackColor = true;
            this.btnFolder.Click += new System.EventHandler(this.btnFolder_Click);
            // 
            // btnFile
            // 
            this.btnFile.Location = new System.Drawing.Point(12, 112);
            this.btnFile.Name = "btnFile";
            this.btnFile.Size = new System.Drawing.Size(212, 23);
            this.btnFile.TabIndex = 5;
            this.btnFile.Text = "Select File...";
            this.btnFile.UseVisualStyleBackColor = true;
            this.btnFile.Click += new System.EventHandler(this.btnFile_Click);
            // 
            // btnExecute
            // 
            this.btnExecute.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnExecute.Location = new System.Drawing.Point(12, 186);
            this.btnExecute.Name = "btnExecute";
            this.btnExecute.Size = new System.Drawing.Size(75, 23);
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
            // chkAutoExecute
            // 
            this.chkAutoExecute.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkAutoExecute.AutoSize = true;
            this.chkAutoExecute.Location = new System.Drawing.Point(93, 190);
            this.chkAutoExecute.Name = "chkAutoExecute";
            this.chkAutoExecute.Size = new System.Drawing.Size(131, 17);
            this.chkAutoExecute.TabIndex = 8;
            this.chkAutoExecute.Text = "Auto-Execute on Drop";
            this.chkAutoExecute.UseVisualStyleBackColor = true;
            // 
            // txtLog
            // 
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.BackColor = System.Drawing.Color.Black;
            this.txtLog.ForeColor = System.Drawing.Color.White;
            this.txtLog.Location = new System.Drawing.Point(230, 142);
            this.txtLog.Name = "txtLog";
            this.txtLog.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(341, 67);
            this.txtLog.TabIndex = 9;
            this.txtLog.Text = "";
            // 
            // TypeExtensionForm
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(583, 221);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.chkAutoExecute);
            this.Controls.Add(this.chkSubDirectories);
            this.Controls.Add(this.btnExecute);
            this.Controls.Add(this.btnFile);
            this.Controls.Add(this.btnFolder);
            this.Controls.Add(this.txtPath);
            this.Controls.Add(this.gbTypeExtensionParameters);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmbExtensions);
            this.Name = "TypeExtensionForm";
            this.Text = "TypeExtensionForm";
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.TypeExtensionForm_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.TypeExtensionForm_DragEnter);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cmbExtensions;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox gbTypeExtensionParameters;
        private System.Windows.Forms.TextBox txtPath;
        private System.Windows.Forms.Button btnFolder;
        private System.Windows.Forms.Button btnFile;
        private System.Windows.Forms.Button btnExecute;
        private System.Windows.Forms.CheckBox chkSubDirectories;
        private System.Windows.Forms.CheckBox chkAutoExecute;
        private System.Windows.Forms.RichTextBox txtLog;
    }
}