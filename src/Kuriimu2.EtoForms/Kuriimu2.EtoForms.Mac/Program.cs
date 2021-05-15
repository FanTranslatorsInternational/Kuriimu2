using System;
using Eto.Forms;
using Kuriimu2.EtoForms.Forms;

namespace Kuriimu2.EtoForms.Mac
{
    class MainClass
    {
        [STAThread]
        public static void Main(string[] args)
        {
            new Application(Eto.Platforms.Mac64).Run(new MainForm());
        }
    }
}
