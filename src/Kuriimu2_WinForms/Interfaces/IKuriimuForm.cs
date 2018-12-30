using Kontract.Interfaces.Archive;
using Kore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kuriimu2_WinForms.Interfaces
{
    internal interface IKuriimuForm
    {
        event EventHandler<CreateTabEventArgs> CreateTab;

        bool HasChanges { get; }
        KoreFileInfo Kfi { get; }

        void Save(string filename = "");
        void Close();
        void UpdateForm2();
    }

    public class CreateTabEventArgs : EventArgs
    {
        public KoreFileInfo Kfi { get; set; }
        public IArchiveAdapter ParentAdapter { get; set; }
        public TabPage ParentTabPage { get; set; }
    }
}
