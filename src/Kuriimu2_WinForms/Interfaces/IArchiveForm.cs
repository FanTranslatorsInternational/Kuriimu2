using Kontract.Interfaces.Archive;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.VirtualFS;
using Kore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kuriimu2_WinForms.Interfaces
{
    public interface IArchiveForm : IKuriimuForm
    {
        event EventHandler<OpenTabEventArgs> OpenTab;

        void UpdateParent();
        void UpdateChildTabs(KoreFileInfo kfi);

        void RemoveChildTab(TabPage tabPage);
    }

    public class OpenTabEventArgs : EventArgs
    {
        public OpenTabEventArgs(ArchiveFileInfo afi, KoreFileInfo kfi, IVirtualFSRoot fs)
        {
            Afi = afi;
            Kfi = kfi;
            FileSystem = fs;
        }

        public ArchiveFileInfo Afi { get; }
        public KoreFileInfo Kfi { get; }
        public IVirtualFSRoot FileSystem { get; }
        public bool LeaveOpen { get; set; }

        public bool EventResult { get; set; }
        public TabPage OpenedTabPage { get; set; }
    }
}
