using System.Collections.Generic;
using Eto.Forms;
using Kontract.Interfaces.Plugins.Identifier;

namespace Kuriimu2.EtoForms.Forms.Dialogs
{
    partial class ChoosePluginDialog : Dialog
    {
        private readonly IReadOnlyList<IFilePlugin> _filePlugins;

        public IFilePlugin SelectedFilePlugin { get; private set; }

        public ChoosePluginDialog(IReadOnlyList<IFilePlugin> filePlugins)
        {
            InitializeComponent();

            _filePlugins = filePlugins;
            AddPlugins();

            okButtonCommand.Executed += OkButtonCommand_Executed;
            cancelButtonCommand.Executed += CancelButtonCommand_Executed;
        }

        private void AddPlugins()
        {
            foreach (var plugin in _filePlugins)
                pluginList.Items.Add(CreateListItem(plugin));
        }

        private ListItem CreateListItem(IFilePlugin plugin)
        {
            var item = new ListItem { Tag = plugin };

            item.Text = plugin.Metadata?.Name ?? "<undefined>";
            item.Key = plugin.PluginId.ToString("D");

            //item.SubItems.Add(plugin.PluginType.ToString());
            //item.SubItems.Add(plugin.PluginId.ToString("D"));

            return item;
        }

        #region Events

        private void CancelButtonCommand_Executed(object sender, System.EventArgs e)
        {
            throw new System.NotImplementedException();
        }

        private void OkButtonCommand_Executed(object sender, System.EventArgs e)
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }
}
