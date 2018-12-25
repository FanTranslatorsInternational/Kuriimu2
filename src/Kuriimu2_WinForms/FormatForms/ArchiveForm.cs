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
using Kontract.Interfaces.Common;
using System.IO;
using Kontract.Interfaces.Archive;
using Kontract.Interfaces.VirtualFS;
using Kontract.Interfaces.Image;
using Kontract.Interfaces.Text;
using Kontract.FileSystem;

namespace Kuriimu2_WinForms.FormatForms
{
    public partial class ArchiveForm : UserControl, IKuriimuForm
    {
        private Kore.Kore _kore;
        private TabControl _tabControl;
        private string _tempFolder;
        private string _subFolder;

        public ArchiveForm(KoreFileInfo kfi, TabControl tabControl, string tempFolder, string subFolder)
        {
            InitializeComponent();

            Kfi = kfi;
            _kore = new Kore.Kore();
            _tabControl = tabControl;
            _tempFolder = tempFolder;
            _subFolder = subFolder;
        }

        public KoreFileInfo Kfi { get; set; }

        private void button1_Click(object sender, EventArgs e)
        {
            var files = (Kfi.Adapter as IArchiveAdapter).Files;

            MessageBox.Show(files.Aggregate("", (a, b) => a + Environment.NewLine + b.FileName), "Files", MessageBoxButtons.OK);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var files = (Kfi.Adapter as IArchiveAdapter).Files;
            var vfs = new VirtualFileSystem(Kfi.Adapter as IArchiveAdapter, Path.Combine(_tempFolder, _subFolder));

            vfs.ExtractFile(files[3].FileName);

            var kfi = _kore.LoadFile(Path.Combine(_tempFolder, _subFolder, Path.GetFileName(files[3].FileName)), vfs);
            AddTabPage(kfi);
        }

        private void AddTabPage(KoreFileInfo kfi)
        {
            var tabPage = new TabPage();

            if (kfi.Adapter is ITextAdapter)
                tabPage.Controls.Add(new TextForm(kfi));
            else if (kfi.Adapter is IImageAdapter)
                tabPage.Controls.Add(new ImageForm(kfi));
            else if (kfi.Adapter is IArchiveAdapter)
                tabPage.Controls.Add(new ArchiveForm(kfi, _tabControl, _tempFolder, Guid.NewGuid().ToString()));

            _tabControl.TabPages.Add(tabPage);
        }
    }
}
