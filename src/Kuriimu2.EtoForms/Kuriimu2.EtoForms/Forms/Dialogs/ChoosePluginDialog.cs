using System.Collections.Generic;
using Eto.Forms;
using Kontract.Interfaces.Plugins.Identifier;

namespace Kuriimu2.EtoForms.Forms.Dialogs
{
    partial class ChoosePluginDialog : Dialog
    {
        public IFilePlugin SelectedFilePlugin { get; private set; }

        public ChoosePluginDialog(IReadOnlyList<IFilePlugin> filePlugins)
        {
            // TODO: Setup up dialog
        }
    }
}
