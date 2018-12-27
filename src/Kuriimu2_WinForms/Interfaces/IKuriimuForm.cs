using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kuriimu2_WinForms.Interfaces
{
    internal interface IKuriimuForm
    {
        bool HasChanges { get; }

        Kore.KoreFileInfo Kfi { get; }

        void Save(string filename = "");

        void Close();
    }
}
