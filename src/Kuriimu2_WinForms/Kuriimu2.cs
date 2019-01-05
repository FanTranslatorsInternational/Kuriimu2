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

        private TabPage AddTabPage(KoreFileInfo kfi, Color tabColor, IArchiveAdapter parentAdapter = null, TabPage parentTabPage = null)
        {
            var tabPage = new TabPage();

            IKuriimuForm tabControl = null;
            if (kfi.Adapter is ITextAdapter)
                tabControl = new TextForm(kfi, tabPage, parentAdapter, parentTabPage);
            else if (kfi.Adapter is IImageAdapter)
                tabControl = new ImageForm(kfi, tabPage, parentAdapter, parentTabPage);
            else if (kfi.Adapter is IArchiveAdapter)
            {
                tabControl = new ArchiveForm(kfi, tabPage, parentAdapter, parentTabPage, _tempFolder);
                (tabControl as IArchiveForm).OpenTab += Kuriimu2_OpenTab;
            }
            tabControl.TabColor = tabColor;

            tabControl.SaveTab += TabControl_SaveTab;
            tabControl.CloseTab += TabControl_CloseTab;

            tabPage.Controls.Add(tabControl as UserControl);

            openFiles.TabPages.Add(tabPage);
            tabPage.ImageKey = "close-button";  // setting ImageKey before adding, makes the image not working
            openFiles.SelectedTab = tabPage;

            return tabPage;
        }

        private void TabControl_SaveTab(object sender, SaveTabEventArgs e)
        {
            if (e.Kfi.HasChanges)
            {
                // Save files
                var ksi = new KoreSaveInfo(e.Kfi, _tempFolder) { Version = e.Version, NewSaveLocation = e.NewSaveLocation };
                _kore.SaveFile2(ksi);
                if (ksi.SavedKfi.ParentKfi != null)
                    ksi.SavedKfi.ParentKfi.HasChanges = true;

                // Update current tab and its childs if possible
                (sender as IKuriimuForm).Kfi = ksi.SavedKfi;
                if (sender is ArchiveForm archiveForm)
                    archiveForm.UpdateChildTabs(ksi.SavedKfi);

                // Update current and parent tabs
                (sender as IKuriimuForm).UpdateForm();
            }
        }

        private void TabControl_CloseTab(object sender, CloseTabEventArgs e)
        {
            CloseTab(e.Kfi, sender as IKuriimuForm, false, e.LeaveOpen, e.ParentTabPage);
        }

        private bool CloseTab(KoreFileInfo kfi, IKuriimuForm form, bool ignoreChildWarning, bool leaveOpen = false, TabPage parentTabPage = null)
        {
            // Security question, so the user knows that every sub file will be closed
            if (kfi.ChildKfi != null && kfi.ChildKfi.Count > 0 && !ignoreChildWarning)
            {
                var result = MessageBox.Show("Every file opened from this one and below will be closed too. Continue?", "Dependant files", MessageBoxButtons.YesNo);
                switch (result)
                {
                    case DialogResult.Yes:
                        break;
                    case DialogResult.No:
                    default:
                        return false;
                }
            }

            // Save unchanged saves, if wanted
            if (kfi.HasChanges)
            {
                var result = MessageBox.Show($"Changes were made to \"{kfi.FullPath}\" or its opened sub files. Do you want to save those changes?", "Unsaved changes", MessageBoxButtons.YesNoCancel);
                switch (result)
                {
                    case DialogResult.Yes:
                        TabControl_SaveTab(form, new SaveTabEventArgs(kfi));
                        break;
                    case DialogResult.No:
                        break;
                    case DialogResult.Cancel:
                    default:
                        return false;
                }
            }

            // Remove all tabs related to KFIs
            CloseOpenTabs(kfi);

            // Update parent, if existent
            if (kfi.ParentKfi != null)
                (parentTabPage.Controls[0] as ArchiveForm).RemoveChildTab(form as ArchiveForm);

            // Close all KFIs
            _kore.CloseFile(kfi, leaveOpen);

            return true;
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
            var openedTabPage = GetTabPageForFullPath(Path.Combine(e.ParentKfi.FullPath, e.AFI.FileName));
            if (openedTabPage == null)
            {
                var newKfi = _kore.LoadFile(new KoreLoadInfo(e.AFI.FileData, e.AFI.FileName) { LeaveOpen = e.LeaveOpen, FileSystem = e.FileSystem });
                if (newKfi == null)
                    return;

                newKfi.ParentKfi = e.ParentKfi;
                var newTabPage = AddTabPage(newKfi, (sender as IKuriimuForm).TabColor, e.ParentKfi.Adapter as IArchiveAdapter, e.ParentTabPage);

                e.NewTabPage = newTabPage;
                e.NewKfi.AFI = e.AFI;
            }
            else
                openFiles.SelectedTab = openedTabPage;

            e.EventResult = true;
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

        private void openFiles_DrawItem(object sender, DrawItemEventArgs e)
        {
            var tabColor = (openFiles.TabPages[e.Index].Controls[0] as IKuriimuForm).TabColor;
            var textColor = (tabColor.GetBrightness() <= 0.5) ? Color.White : Color.Black;

            // Color the Tab Header
            e.Graphics.FillRectangle(new SolidBrush(tabColor), e.Bounds);

            // Format String
            var drawFormat = new StringFormat();
            drawFormat.Alignment = StringAlignment.Far;
            drawFormat.LineAlignment = StringAlignment.Center;

            // Draw Header Text
            e.Graphics.DrawString(openFiles.TabPages[e.Index].Text, e.Font, new SolidBrush(textColor), new Rectangle(e.Bounds.Left, e.Bounds.Top, e.Bounds.Width - 2, e.Bounds.Height), drawFormat);

            //Draw image
            var drawPoint = (openFiles.SelectedIndex == e.Index) ? new Point(e.Bounds.Left + 9, e.Bounds.Top + 4) : new Point(e.Bounds.Left + 3, e.Bounds.Top + 2);
            e.Graphics.DrawImage(tabCloseButtons.Images["close-button"], drawPoint);
        }

        private void Kuriimu2_FormClosing(object sender, FormClosingEventArgs e)
        {
            while (openFiles.TabPages.Count > 0)
                if (!CloseTab((openFiles.TabPages[0].Controls[0] as IKuriimuForm).Kfi, openFiles.TabPages[0].Controls[0] as IKuriimuForm, true))
                {
                    e.Cancel = true;
                    return;
                }
        }
    }
}
