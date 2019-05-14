using Kontract.Attributes;
using Kontract.Interfaces.Archive;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.FileSystem;
using Kore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kontract.FileSystem.Nodes.Abstract;
using Kontract.Interfaces;

namespace Kuriimu2_WinForms.Interfaces
{
    public interface IArchiveForm : IKuriimuForm
    {
        event EventHandler<OpenTabEventArgs> OpenTab;
        event EventHandler<GetAdapterInformationByIdEventArgs> GetAdapterById;

        void UpdateParent();
        void UpdateChildTabs(KoreFileInfo kfi);

        void RemoveChildTab(TabPage tabPage);
    }

    public class OpenTabEventArgs : EventArgs
    {
        public OpenTabEventArgs(ArchiveFileInfo afi, KoreFileInfo kfi, BaseReadOnlyDirectoryNode fs)
        {
            Afi = afi;
            Kfi = kfi;
            FileSystem = fs;
        }

        public ArchiveFileInfo Afi { get; }
        public KoreFileInfo Kfi { get; }
        public BaseReadOnlyDirectoryNode FileSystem { get; }
        public ILoadFiles PreselectedAdapter { get; set; }
        public bool LeaveOpen { get; set; }

        public bool EventResult { get; set; }
        public TabPage OpenedTabPage { get; set; }
    }

    public class GetAdapterInformationByIdEventArgs : EventArgs
    {
        public string PluginName { get; }

        public ILoadFiles SelectedPlugin { get; set; }
        public PluginInfoAttribute PluginMetaData { get; set; }

        public GetAdapterInformationByIdEventArgs(string pluginName)
        {
            PluginName = pluginName;
        }
    }
}
