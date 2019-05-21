using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kontract.Interfaces.Archive;
using Kontract.Interfaces.Layout;
using Kontract.Models;
using Kore.Files.Models;
using Kuriimu2_WinForms.Interfaces;

namespace Kuriimu2_WinForms.FormatForms
{
    public partial class LayoutForm : UserControl, IKuriimuForm
    {
        private IArchiveAdapter _parentAdapter;
        private TabPage _currentTab;
        private TabPage _parentTab;

        private ILayoutAdapter _layoutAdapter => (ILayoutAdapter)Kfi.Adapter;

        public event EventHandler<SaveTabEventArgs> SaveTab;
        public event EventHandler<CloseTabEventArgs> CloseTab;
        public event EventHandler<ProgressReport> ReportProgress;

        public KoreFileInfo Kfi { get; set; }

        public Color TabColor { get; set; }

        public LayoutForm(KoreFileInfo kfi, TabPage tabPage, IArchiveAdapter parentAdapter, TabPage parentTabPage)
        {
            InitializeComponent();

            Kfi = kfi;
            _currentTab = tabPage;
            _parentTab = parentTabPage;
            _parentAdapter = parentAdapter;

            imgLayout.Image = new Bitmap(500, 500);

            UpdateForm();
            UpdateLayout();
        }

        public void Save(string filename = "")
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void UpdateForm()
        {
            _currentTab.Text = Kfi.DisplayName;
        }

        private void UpdateLayout()
        {
            var g = Graphics.FromImage(imgLayout.Image);
            _layoutAdapter.Layout.Draw(g);
        }
    }
}
