using Kontract.Interfaces.Archive;
using Kore;
using Kuriimu2_WinForms.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kuriimu2_WinForms.FormatForms
{
    public partial class TextForm : UserControl, IKuriimuForm
    {
        private TabPage _tabPage;
        private TabPage _parentTabPage;
        private IArchiveAdapter _parentAdapter;

        public TextForm(KoreFileInfo kfi, TabPage tabPage, IArchiveAdapter parentAdapter, TabPage parentTabPage)
        {
            InitializeComponent();

            Kfi = kfi;

            _tabPage = tabPage;
            _parentTabPage = parentTabPage;
            _parentAdapter = parentAdapter;
        }

        public KoreFileInfo Kfi { get; private set; }

        public bool HasChanges => throw new NotImplementedException();

        public event EventHandler<CreateTabEventArgs> CreateTab;

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
