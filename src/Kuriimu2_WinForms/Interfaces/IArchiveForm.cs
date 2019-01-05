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
    }

    public class OpenTabEventArgs : EventArgs
    {
        public OpenTabEventArgs(ArchiveFileInfo afi, IVirtualFSRoot fs, TabPage parentTab)
        {
            AFI = afi;
            FileSystem = fs;
            ParentTabPage = parentTab;
        }

        public ArchiveFileInfo AFI { get; }
        public IVirtualFSRoot FileSystem { get; }
        public TabPage ParentTabPage { get; }
        public KoreFileInfo ParentKfi { get => (ParentTabPage.Controls[0] as IKuriimuForm).Kfi; }
        public bool LeaveOpen { get; set; }

        public bool EventResult { get; set; }
        public TabPage NewTabPage { get; set; }
        public KoreFileInfo NewKfi { get => (NewTabPage.Controls[0] as IKuriimuForm).Kfi; }
    }
}
