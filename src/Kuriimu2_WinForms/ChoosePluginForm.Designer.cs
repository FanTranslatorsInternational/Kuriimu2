namespace Kuriimu2_WinForms
{
    partial class ChoosePluginForm
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
            this.pluginList = new System.Windows.Forms.ListView();
            this.PluginId = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.PluginType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.splMain = new System.Windows.Forms.SplitContainer();
            this.okBtn = new System.Windows.Forms.Button();
            this.cancelBtn = new System.Windows.Forms.Button();
            this.pluginName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            ((System.ComponentModel.ISupportInitialize)(this.splMain)).BeginInit();
            this.splMain.Panel1.SuspendLayout();
            this.splMain.Panel2.SuspendLayout();
            this.splMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // pluginList
            // 
            this.pluginList.Activation = System.Windows.Forms.ItemActivation.OneClick;
            this.pluginList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.PluginId,
            this.pluginName,
            this.PluginType});
            this.pluginList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pluginList.FullRowSelect = true;
            this.pluginList.Location = new System.Drawing.Point(0, 0);
            this.pluginList.MultiSelect = false;
            this.pluginList.Name = "pluginList";
            this.pluginList.ShowGroups = false;
            this.pluginList.ShowItemToolTips = true;
            this.pluginList.Size = new System.Drawing.Size(312, 367);
            this.pluginList.TabIndex = 0;
            this.pluginList.UseCompatibleStateImageBehavior = false;
            this.pluginList.SelectedIndexChanged += new System.EventHandler(this.pluginList_SelectedIndexChanged);
            this.pluginList.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.pluginList_MouseDoubleClick);
            // 
            // PluginId
            // 
            this.PluginId.Text = "Id";
            // 
            // PluginType
            // 
            this.PluginType.DisplayIndex = 1;
            this.PluginType.Text = "Type";
            // 
            // splMain
            // 
            this.splMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splMain.IsSplitterFixed = true;
            this.splMain.Location = new System.Drawing.Point(0, 0);
            this.splMain.Name = "splMain";
            this.splMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splMain.Panel1
            // 
            this.splMain.Panel1.Controls.Add(this.pluginList);
            // 
            // splMain.Panel2
            // 
            this.splMain.Panel2.Controls.Add(this.okBtn);
            this.splMain.Panel2.Controls.Add(this.cancelBtn);
            this.splMain.Size = new System.Drawing.Size(312, 408);
            this.splMain.SplitterDistance = 367;
            this.splMain.TabIndex = 1;
            // 
            // okBtn
            // 
            this.okBtn.Dock = System.Windows.Forms.DockStyle.Right;
            this.okBtn.Location = new System.Drawing.Point(162, 0);
            this.okBtn.Name = "okBtn";
            this.okBtn.Size = new System.Drawing.Size(75, 37);
            this.okBtn.TabIndex = 1;
            this.okBtn.Text = "Ok";
            this.okBtn.UseVisualStyleBackColor = true;
            this.okBtn.Click += new System.EventHandler(this.okBtn_Click);
            // 
            // cancelBtn
            // 
            this.cancelBtn.Dock = System.Windows.Forms.DockStyle.Right;
            this.cancelBtn.Location = new System.Drawing.Point(237, 0);
            this.cancelBtn.Name = "cancelBtn";
            this.cancelBtn.Size = new System.Drawing.Size(75, 37);
            this.cancelBtn.TabIndex = 0;
            this.cancelBtn.Text = "Cancel";
            this.cancelBtn.UseVisualStyleBackColor = true;
            this.cancelBtn.Click += new System.EventHandler(this.cancelBtn_Click);
            // 
            // pluginName
            // 
            this.pluginName.DisplayIndex = 2;
            this.pluginName.Text = "Name";
            // 
            // ChoosePluginForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(312, 408);
            this.Controls.Add(this.splMain);
            this.Name = "ChoosePluginForm";
            this.Text = "Choose plugin...";
            this.splMain.Panel1.ResumeLayout(false);
            this.splMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splMain)).EndInit();
            this.splMain.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView pluginList;
        private System.Windows.Forms.ColumnHeader PluginId;
        private System.Windows.Forms.ColumnHeader PluginType;
        private System.Windows.Forms.SplitContainer splMain;
        private System.Windows.Forms.Button okBtn;
        private System.Windows.Forms.Button cancelBtn;
        private System.Windows.Forms.ColumnHeader pluginName;
    }
}