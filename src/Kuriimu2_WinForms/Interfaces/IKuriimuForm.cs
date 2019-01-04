using Kontract.Interfaces.Archive;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.VirtualFS;
using Kore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kuriimu2_WinForms.Interfaces
{
    internal interface IKuriimuForm
    {
        event EventHandler<OpenTabEventArgs> OpenTab;
        event EventHandler<SaveTabEventArgs> SaveTab;
        event EventHandler<CloseTabEventArgs> CloseTab;

        KoreFileInfo Kfi { get; set; }
        Color TabColor { get; set; }

        void Save(string filename = "");
        void Close();
        void UpdateForm();
    }

    public class OpenTabEventArgs : EventArgs
    {
        public OpenTabEventArgs(StreamInfo info, IVirtualFSRoot fs)
        {
            StreamInfo = info;
            FileSystem = fs;
        }

        public StreamInfo StreamInfo { get; }
        public IVirtualFSRoot FileSystem { get; }
        public bool LeaveOpen { get; set; }
        public KoreFileInfo ParentKfi { get; set; }
        public TabPage ParentTabPage { get; set; }

        public KoreFileInfo NewKfi { get; set; }
        public TabPage NewTabPage { get; set; }
    }

    public class SaveTabEventArgs : EventArgs
    {
        public SaveTabEventArgs(KoreFileInfo kfi)
        {
            Kfi = kfi;
        }

        public KoreFileInfo Kfi { get; }
        public string NewSaveLocation { get; set; }
        public int Version { get; set; }
    }

    public class CloseTabEventArgs : EventArgs
    {
        public CloseTabEventArgs(KoreFileInfo kfi, TabPage parentTabPage)
        {
            Kfi = kfi;
            ParentTabPage = parentTabPage;
        }

        public KoreFileInfo Kfi { get; }
        public TabPage ParentTabPage { get; }
        public bool LeaveOpen { get; set; }
    }
}
