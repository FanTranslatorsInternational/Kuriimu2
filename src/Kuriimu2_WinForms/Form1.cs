using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kuriimu2_WinForms.FormatForms;
using Kontract.Interfaces.Text;
using Kontract.Interfaces.Image;
using Kontract.Interfaces.Archive;

namespace Kuriimu2_WinForms
{
    public partial class Form1 : Form
    {
        private Kore.Kore _kore;

        public Form1()
        {
            InitializeComponent();
            _kore = new Kore.Kore();
        }

        #region Events
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog() { Filter = _kore.FileFilters };
            if (ofd.ShowDialog() == DialogResult.OK)
                OpenFile(ofd.FileName);
        }
        #endregion

        #region Methods
        private void OpenFile(string filename)
        {
            if (!File.Exists(filename))
                throw new FileLoadException(filename);

            var kfi = _kore.LoadFile(filename);
            AddTabPage(kfi);
        }

        private void AddTabPage(Kore.KoreFileInfo kfi)
        {
            var tabPage = new TabPage();

            if (kfi.Adapter is ITextAdapter)
                tabPage.Controls.Add(new TextForm(kfi));
            else if (kfi.Adapter is IImageAdapter)
                tabPage.Controls.Add(new ImageForm(kfi));
            else if (kfi.Adapter is IArchiveAdapter)
                tabPage.Controls.Add(new ArchiveForm(kfi));

            openFiles.TabPages.Add(tabPage);
        }
        #endregion
    }
}
