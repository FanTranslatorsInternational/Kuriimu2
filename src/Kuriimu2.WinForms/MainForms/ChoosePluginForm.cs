using System;
using System.Linq;
using System.Windows.Forms;
using Kontract.Interfaces.Loaders;
using Kontract.Interfaces.Plugins.Identifier;

namespace Kuriimu2.WinForms.MainForms
{
    public partial class ChoosePluginForm : Form
    {
        private readonly IPluginLoader<IFilePlugin>[] _pluginLoaders;

        public Guid SelectedPluginId { get; private set; }

        public ChoosePluginForm(params IPluginLoader<IFilePlugin>[] filePluginLoaders)
        {
            InitializeComponent();

            _pluginLoaders = filePluginLoaders;

            AddPlugins();
        }

        private void AddPlugins()
        {
            foreach (var plugin in _pluginLoaders.SelectMany(x => x.Plugins))
            {
                var metadataName = plugin.Metadata?.Name ?? "<undefined>";
                var listViewItem = new ListViewItem(new[] { plugin.PluginId.ToString("D"), metadataName })
                {
                    Tag = plugin.PluginId
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
                SelectedPluginId = (Guid)pluginList.SelectedItems[0].Tag;
        }

        private void pluginList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            DialogResult = DialogResult.OK;

            Close();
        }
    }
}
