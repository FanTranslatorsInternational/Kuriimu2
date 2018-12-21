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

namespace Kuriimu2_WinForms
{
    public partial class Kuriimu2 : Form
    {
        private Kore.Kore _kore;
        private string _tempOpenDir;

        public Kuriimu2()
        {
            InitializeComponent();

            _kore = new Kore.Kore();
        }

        #region Events
        /// <summary>
        /// When a file gets loaded, this event ensures possible file requests by the plugin are handled
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _kore_RequestFile(object sender, RequestFileEventArgs e)
        {
            var allFiles = EnumerateAllFiles(_tempOpenDir);

            var reg = new Regex(Path.Combine(_tempOpenDir, e.FilePathPattern).Replace("\\", "\\\\"));
            var matches = allFiles.Where(x => reg.IsMatch(x));

            e.OpenedStreamInfos = matches.Select(x => new StreamInfo
            {
                FileData = File.Open(x, FileMode.Open),
                FileName = x
            }).ToArray();
        }

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

            _kore.RequestFiles += _kore_RequestFile;
            _tempOpenDir = Path.GetDirectoryName(filename);
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

        private IEnumerable<string> EnumerateAllFiles(string rootDir)
        {
            var files = Directory.EnumerateFiles(rootDir).ToList(); ;
            var dirs = Directory.EnumerateDirectories(rootDir);
            foreach (var dir in dirs)
                files.AddRange(EnumerateAllFiles(dir));
            return files;
        }
        #endregion
    }
}
