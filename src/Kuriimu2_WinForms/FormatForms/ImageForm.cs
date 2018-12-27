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
    public partial class ImageForm : UserControl, IKuriimuForm
    {
        public ImageForm(KoreFileInfo kfi)
        {
            InitializeComponent();

            Kfi = kfi;
        }

        public KoreFileInfo Kfi { get; set; }

        public bool HasChanges => throw new NotImplementedException();

        public event EventHandler<EventArgs> HasChangesHandler;

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void Save(string filename = "")
        {
            throw new NotImplementedException();
        }
    }
}
