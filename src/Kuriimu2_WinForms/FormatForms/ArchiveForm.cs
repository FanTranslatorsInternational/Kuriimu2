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
    }
}
