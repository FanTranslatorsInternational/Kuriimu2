using Kontract.Interfaces.Archive;
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

        KoreFileInfo Kfi { get; }
        Color TabColor { get; set; }

        void Save(string filename = "");
        void Close();
        void UpdateForm2();
    }

    public class OpenTabEventArgs : EventArgs
    {
        public KoreFileInfo Kfi { get; set; }
        public IArchiveAdapter ParentAdapter { get; set; }
        public TabPage ParentTabPage { get; set; }
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
        public CloseTabEventArgs(KoreFileInfo kfi)
        {
            Kfi = kfi;
        }

        public KoreFileInfo Kfi { get; }
        public bool LeaveOpen { get; set; }
    }
}
