using Kontract;
using Kontract.Attributes;
using Kontract.Interfaces.Archive;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Font;
using Kontract.Interfaces.Image;
using Kontract.Interfaces.Text;
using Kore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kuriimu2_WinForms
{
    public partial class ChoosePluginForm : Form
    {
        private readonly PluginLoader _pluginLoader;

        public ILoadFiles ChosenAdapter { get; private set; }

        public ChoosePluginForm(PluginLoader loader)
        {
            InitializeComponent();

            _pluginLoader = loader;

            AddAdapters<IArchiveAdapter>("Archive");
            AddAdapters<IImageAdapter>("Image");
            AddAdapters<ITextAdapter>("Text");
            AddAdapters<IFontAdapter>("Font");
        }

        private void AddAdapters<T>(string typeName) where T : IPlugin
        {
            foreach (var adapter in _pluginLoader.GetAdapters<T>().Where(x => x is ILoadFiles))
            {
                var meta = _pluginLoader.GetMetadata<PluginInfoAttribute>(adapter);
                if (meta != null)
                    pluginList.Items.Add(new ListViewItem(new[] { meta.ID, meta.Name, typeName }) { Tag = adapter });
                else
                    pluginList.Items.Add(new ListViewItem(new[] { "<not given>", "<not given>", typeName }) { Tag = adapter });
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
                ChosenAdapter = (ILoadFiles)pluginList.SelectedItems[0].Tag;
        }

        private void pluginList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
