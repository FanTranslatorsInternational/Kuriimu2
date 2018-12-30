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
using Kontract.Interfaces.Common;
using System.Text.RegularExpressions;
using Kontract.Interfaces.VirtualFS;
using Kontract.FileSystem;
using Kuriimu2_WinForms.Interfaces;
using Kuriimu2_WinForms.FormatForms.Archive;
using Kore;

namespace Kuriimu2_WinForms
{
    public partial class Kuriimu2 : Form
    {
        private KoreManager _kore;
        private string _tempFolder = "temp";

        public Kuriimu2()
        {
            InitializeComponent();

            _kore = new KoreManager();
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

            var kfi = _kore.LoadFile(new KoreLoadInfo(File.Open(filename, FileMode.Open), filename) { FileSystem = new PhysicalFileSystem(Path.GetDirectoryName(filename)) });
            AddTabPage(kfi);
        }

        private void AddTabPage(KoreFileInfo kfi, IArchiveAdapter parentAdapter = null, TabPage parentTabPage = null)
        {
            var tabPage = new TabPage();

            IKuriimuForm tabControl = null;
            if (kfi.Adapter is ITextAdapter)
                tabControl = new TextForm(kfi, tabPage, parentAdapter, parentTabPage);
            else if (kfi.Adapter is IImageAdapter)
                tabControl = new ImageForm(kfi, tabPage, parentAdapter, parentTabPage);
            else if (kfi.Adapter is IArchiveAdapter)
                tabControl = new ArchiveForm(kfi, tabPage, parentAdapter, parentTabPage, _tempFolder);
            tabControl.CreateTab += Kuriimu2_CreateTab;

            tabPage.Controls.Add(tabControl as UserControl);

            openFiles.TabPages.Add(tabPage);
        }

        private void Kuriimu2_CreateTab(object sender, CreateTabEventArgs e)
        {
            AddTabPage(e.Kfi, e.ParentAdapter, e.ParentTabPage);
        }
        #endregion

        private void openFiles_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.Graphics.DrawString("x", e.Font, Brushes.Black, e.Bounds.Right - 15, e.Bounds.Top + 4);
            e.Graphics.DrawString(openFiles.TabPages[e.Index].Text, e.Font, Brushes.Black, e.Bounds.Left + 12, e.Bounds.Top + 4);
            e.DrawFocusRectangle();
        }

        private void openFiles_MouseUp(object sender, MouseEventArgs e)
        {
            Rectangle r = openFiles.GetTabRect(openFiles.SelectedIndex);
            Rectangle closeButton = new Rectangle(r.Right - 15, r.Top + 4, 9, 7);
            if (closeButton.Contains(e.Location))
            {
                foreach (Control control in openFiles.SelectedTab.Controls)
                    if (control is IKuriimuForm kuriimuTab)
                        kuriimuTab.Close();
                openFiles.TabPages.Remove(openFiles.SelectedTab);
            }
        }
    }
}
