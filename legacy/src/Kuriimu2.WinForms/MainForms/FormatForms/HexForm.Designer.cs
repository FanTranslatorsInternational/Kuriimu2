using System.Windows.Forms;

namespace Kuriimu2.WinForms.MainForms.FormatForms
{
    partial class HexForm
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
            this.fileData = new Be.Windows.Forms.HexBox();
            this.SuspendLayout();
            // 
            // fileData
            // 
            this.fileData.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.fileData.ColumnInfoVisible = true;
            this.fileData.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.fileData.GroupSeparatorVisible = true;
            this.fileData.LineInfoVisible = true;
            this.fileData.Location = new System.Drawing.Point(3, 3);
            this.fileData.Name = "fileData";
            this.fileData.ShadowSelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(60)))), ((int)(((byte)(188)))), ((int)(((byte)(255)))));
            this.fileData.Size = new System.Drawing.Size(144, 144);
            this.fileData.StringViewVisible = true;
            this.fileData.TabIndex = 0;
            this.fileData.UseFixedBytesPerLine = true;
            this.fileData.VScrollBarVisible = true;
            // 
            // HexForm
            // 
            this.Dock = DockStyle.Fill;
            this.Controls.Add(this.fileData);
            this.Name = "HexForm";
            this.ResumeLayout(false);

        }

        #endregion

        private Be.Windows.Forms.HexBox fileData;
    }
}
