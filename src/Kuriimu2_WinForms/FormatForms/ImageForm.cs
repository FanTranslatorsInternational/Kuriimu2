using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kuriimu2_WinForms.Interfaces;
using Kore;
using Kontract.Interfaces.Archive;

namespace Kuriimu2_WinForms.FormatForms
{
    public partial class ImageForm : UserControl, IKuriimuForm
    {
        private TabPage _tabPage;
        private TabPage _parentTabPage;
        private IArchiveAdapter _parentAdapter;

        public ImageForm(KoreFileInfo kfi, TabPage tabPage, IArchiveAdapter parentAdapter, TabPage parentTabPage)
        {
            InitializeComponent();

            Kfi = kfi;

            _tabPage = tabPage;
            _parentTabPage = parentTabPage;
            _parentAdapter = parentAdapter;
        }

        public KoreFileInfo Kfi { get; private set; }

        public bool HasChanges { get; private set; }
        public Color TabColor { get; set; }
        
        public event EventHandler<OpenTabEventArgs> OpenTab;
        public event EventHandler<SaveTabEventArgs> SaveTab;
        public event EventHandler<CloseTabEventArgs> CloseTab;

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void Save(string filename = "")
        {
            throw new NotImplementedException();
        }

        public void UpdateForm2()
        {
            throw new NotImplementedException();
        }
    }
}
