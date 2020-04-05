using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Kontract.Interfaces.Plugins.Identifier;

namespace Kuriimu2.WinForms.MainForms
{
    public partial class ChoosePluginForm : Form
    {
        private readonly IReadOnlyList<IFilePlugin> _filePlugins;

        public IFilePlugin SelectedFilePlugin { get; private set; }

        public ChoosePluginForm(IReadOnlyList<IFilePlugin> filePlugins)
        {
            InitializeComponent();

            _filePlugins = filePlugins;

            AddPlugins();
        }

        private void AddPlugins()
        {
            foreach (var plugin in _filePlugins)
            {
                var metadataName = plugin.Metadata?.Name ?? "<undefined>";
                var listViewItem = new ListViewItem(new[] { plugin.PluginId.ToString("D"), metadataName })
                {
                    Tag = plugin
                };

                pluginList.Items.Add(listViewItem);
            }
        }

        private void okBtn_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;

            Close();
        }

        private void cancelBtn_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;

            Close();
        }

        private void pluginList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (pluginList.SelectedItems.Count > 0)
                SelectedFilePlugin = (IFilePlugin)pluginList.SelectedItems[0].Tag;
        }

        private void pluginList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            DialogResult = DialogResult.OK;

            Close();
        }
    }
}
