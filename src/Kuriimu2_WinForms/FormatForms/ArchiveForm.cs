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

namespace Kuriimu2_WinForms.FormatForms
{
    public partial class ArchiveForm : UserControl, IKuriimuForm
    {
        public ArchiveForm(KoreFileInfo kfi)
        {
            InitializeComponent();

            Kfi = kfi;
        }

        public KoreFileInfo Kfi { get; set; }

        #region Events
        /// <summary>
        /// When a file from the archive gets loaded, this event ensures possible file requests by the plugin are handled in the context of the archives FS
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _kore_RequestFile(object sender, RequestFileEventArgs e)
        {
            //TODO: Rewrite for archive needs
            //e.SelectedStreamInfo = new StreamInfo
            //{
            //    FileData = File.Open(e.FileName, FileMode.Open),
            //    FileName = e.FileName
            //};
        }
        #endregion
    }
}
