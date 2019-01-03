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
using Kuriimu2_WinForms.Properties;

namespace Kuriimu2_WinForms
{
    public partial class Kuriimu2 : Form
    {
        private Random _rand = new Random();
        private KoreManager _kore;
        private string _tempFolder = "temp";

        public Kuriimu2()
        {
            InitializeComponent();

            _kore = new KoreManager();

            tabCloseButtons.Images.Add(Resources.menu_delete);
            tabCloseButtons.Images.SetKeyName(0, "close-button");
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

            var openedTabPage = GetTabPageForFullPath(filename);
            if (openedTabPage != null)
                openFiles.SelectedTab = openedTabPage;
            else
            {
                var openFile = File.Open(filename, FileMode.Open);
                var kfi = _kore.LoadFile(new KoreLoadInfo(openFile, filename) { FileSystem = new PhysicalFileSystem(Path.GetDirectoryName(filename)) });
                if (kfi == null)
                {
                    MessageBox.Show($"No plugin supports \"{filename}\".");
                    openFile.Dispose();
                    return;
                }

                AddTabPage(kfi, Color.FromArgb(_rand.Next(256), _rand.Next(256), _rand.Next(256)));
            }
        }

        private void AddTabPage(KoreFileInfo kfi, Color tabColor, IArchiveAdapter parentAdapter = null, TabPage parentTabPage = null)
        {
            var tabPage = new TabPage();

            IKuriimuForm tabControl = null;
            if (kfi.Adapter is ITextAdapter)
                tabControl = new TextForm(kfi, tabPage, parentAdapter, parentTabPage);
            else if (kfi.Adapter is IImageAdapter)
                tabControl = new ImageForm(kfi, tabPage, parentAdapter, parentTabPage);
            else if (kfi.Adapter is IArchiveAdapter)
                tabControl = new ArchiveForm(kfi, tabPage, parentAdapter, parentTabPage, _tempFolder);
            tabControl.TabColor = tabColor;

            tabControl.OpenTab += Kuriimu2_OpenTab;
            tabControl.SaveTab += TabControl_SaveTab;
            tabControl.CloseTab += TabControl_CloseTab;

            tabPage.Controls.Add(tabControl as UserControl);

            openFiles.TabPages.Add(tabPage);
            tabPage.ImageKey = "close-button";  // setting ImageKey before adding, makes the image not working
            openFiles.SelectedTab = tabPage;
        }

        private void TabControl_SaveTab(object sender, SaveTabEventArgs e)
        {
            if (e.Kfi.HasChanges)
                _kore.SaveFile(new KoreSaveInfo(e.Kfi, _tempFolder) { Version = e.Version });
        }

        private void TabControl_CloseTab(object sender, CloseTabEventArgs e)
        {
            // Security question, so the user knows that every sub file will be closed
            if (e.Kfi.ChildKfi != null && e.Kfi.ChildKfi.Count > 0)
            {
                var result = MessageBox.Show("Every file opened from this one and below will be closed too. Continue?", "Dependant files", MessageBoxButtons.YesNo);
                switch (result)
                {
                    case DialogResult.Yes:
                        break;
                    case DialogResult.No:
                    default:
                        return;
                }
            }

            // Save unchanged saves, if wanted
            if (e.Kfi.HasChanges)
            {
                var result = MessageBox.Show($"Changes were made to \"{e.Kfi.FullPath}\" or its opened sub files. Do you want to save those changes?", "Unsaved changes", MessageBoxButtons.YesNoCancel);
                switch (result)
                {
                    case DialogResult.Yes:
                        TabControl_SaveTab(sender, new SaveTabEventArgs(e.Kfi));
                        break;
                    case DialogResult.No:
                        break;
                    case DialogResult.Cancel:
                    default:
                        return;
                }
            }

            // Remove all tabs related to KFIs
            CloseOpenTabs(e.Kfi);

            // Update parent, if existent
            if (e.Kfi.ParentKfi != null)
                (e.ParentTabPage.Controls[0] as ArchiveForm).RemoveChildTab(sender as ArchiveForm);

            // Close all KFIs
            _kore.CloseFile(e.Kfi, e.LeaveOpen);
            //TODO: Can this be adopted into Kore.CloseFile?
            if (e.Kfi.ParentKfi != null)
                e.Kfi.ParentKfi.ChildKfi.Remove(e.Kfi);
        }

        private void CloseOpenTabs(KoreFileInfo kfi)
        {
            if (kfi.ChildKfi != null)
                foreach (var child in kfi.ChildKfi)
                    CloseOpenTabs(child);

            List<TabPage> toRemove = new List<TabPage>();
            foreach (TabPage page in openFiles.TabPages)
                foreach (IKuriimuForm kuriimuForm in page.Controls)
                    if (kuriimuForm.Kfi == kfi)
                        toRemove.Add(page);
            foreach (var toRemoveElement in toRemove)
                openFiles.TabPages.Remove(toRemoveElement);
        }

        private void Kuriimu2_OpenTab(object sender, OpenTabEventArgs e)
        {
            var openedTabPage = GetTabPageForFullPath(Path.Combine(e.ParentKfi.FullPath, e.StreamInfo.FileName));
            if (openedTabPage == null)
            {
                var newKfi = _kore.LoadFile(new KoreLoadInfo(e.StreamInfo.FileData, e.StreamInfo.FileName) { LeaveOpen = e.LeaveOpen, FileSystem = e.FileSystem });
                AddTabPage(newKfi, (sender as IKuriimuForm).TabColor, e.ParentKfi.Adapter as IArchiveAdapter, e.ParentTabPage);
                e.NewKfi = newKfi;
            }
            else
                openFiles.SelectedTab = openedTabPage;
        }

        private TabPage GetTabPageForFullPath(string fullPath)
        {
            var openedKfi = _kore.GetOpenedFile(fullPath);
            if (openedKfi == null) return null;

            foreach (TabPage page in openFiles.TabPages)
                foreach (IKuriimuForm kuriimuForm in page.Controls)
                    if (kuriimuForm.Kfi == openedKfi)
                        return page;

            return null;
        }
        #endregion

        private void openFiles_MouseUp(object sender, MouseEventArgs e)
        {
            Rectangle r = openFiles.GetTabRect(openFiles.SelectedIndex);
            Rectangle closeButton = new Rectangle(r.Left + 9, r.Top + 4, tabCloseButtons.Images["close-button"].Width, tabCloseButtons.Images["close-button"].Height);
            if (closeButton.Contains(e.Location))
            {
                foreach (Control control in openFiles.SelectedTab.Controls)
                    if (control is IKuriimuForm kuriimuTab)
                        kuriimuTab.Close();
            }
        }

        private void Kuriimu2_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            foreach (var file in files)
                if (File.Exists(file))
                    OpenFile(file);
        }

        private void Kuriimu2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }
    }
}
