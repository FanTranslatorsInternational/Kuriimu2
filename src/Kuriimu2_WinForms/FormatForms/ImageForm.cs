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
using Kontract.Interfaces.Image;

namespace Kuriimu2_WinForms.FormatForms
{
    public partial class ImageForm : UserControl, IKuriimuForm
    {
        private IArchiveAdapter _parentAdapter;
        private TabPage _currentTab;
        private TabPage _parentTab;

        public ImageForm(KoreFileInfo kfi, TabPage tabPage, IArchiveAdapter parentAdapter, TabPage parentTabPage)
        {
            InitializeComponent();

            Kfi = kfi;

            _currentTab = tabPage;
            _parentTab = parentTabPage;
            _parentAdapter = parentAdapter;

            pictureBox1.Image = (kfi.Adapter as IImageAdapter).BitmapInfos[0].Image;

            UpdateForm();
        }

        public KoreFileInfo Kfi { get; set; }
        public Color TabColor { get; set; }

        public event EventHandler<SaveTabEventArgs> SaveTab;
        public event EventHandler<CloseTabEventArgs> CloseTab;

        public void Close()
        {
            ;
        }

        public void Save(string filename = "")
        {
            ;
        }

        public void UpdateForm()
        {
            _currentTab.Text = Kfi.DisplayName;
        }

        private void ImageForm_Load(object sender, EventArgs e)
        {
            Dock = DockStyle.Fill;
            Padding = new Padding(3);
        }
    }
}
