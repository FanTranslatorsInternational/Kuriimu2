using Kontract;
using Kontract.Interfaces.Archive;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.FileSystem;
using Kore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kontract.Models;
using Kore.Files.Models;

namespace Kuriimu2_WinForms.Interfaces
{
    public interface IKuriimuForm
    {
        event EventHandler<SaveTabEventArgs> SaveTab;
        event EventHandler<CloseTabEventArgs> CloseTab;
        event EventHandler<ProgressReport> ReportProgress;

        KoreFileInfo Kfi { get; set; }
        Color TabColor { get; set; }

        void Save(string filename = "");
        void Close();
        void UpdateForm();
    }

    public class SaveTabEventArgs : EventArgs
    {
        public SaveTabEventArgs(KoreFileInfo kfi)
        {
            Kfi = kfi;
        }

        public KoreFileInfo Kfi { get; }
        public string NewSaveFile { get; set; }
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

        public bool EventResult { get; set; }
    }
}
