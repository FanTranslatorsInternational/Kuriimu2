using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using Kuriimu2.EtoForms.Forms;
using Application = Eto.Forms.Application;

namespace Kuriimu2.EtoForms.Wpf
{
    class MainClass
    {
        

        [STAThread]
        public static void Main(string[] args)
        {
            
            Support.Themer.LoadThemes();
            WpfThemer.LoadThemesWpf();

            // https://stackoverflow.com/a/39348804/10434371
            ToolTipService.ShowOnDisabledProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(true));

            new Application(Eto.Platforms.Wpf).Run(new MainForm());
            
            
        }
    }
}
