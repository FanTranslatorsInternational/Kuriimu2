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
            System.Windows.Media.SolidColorBrush backgroundColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)(Support.Themer.GetTheme().mainColor.A * 255.0), (byte)(Support.Themer.GetTheme().mainColor.R * 255.0), (byte)(Support.Themer.GetTheme().mainColor.G * 255.0), (byte)(Support.Themer.GetTheme().mainColor.B * 255.0)));
            System.Windows.Media.SolidColorBrush foregroundColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)(Support.Themer.GetTheme().altColor.A * 255.0), (byte)(Support.Themer.GetTheme().altColor.R * 255.0), (byte)(Support.Themer.GetTheme().altColor.G * 255.0), (byte)(Support.Themer.GetTheme().altColor.B * 255.0)));

            Eto.Style.Add<Eto.Wpf.Forms.Menu.ButtonMenuItemHandler>(null, handler =>
            {
                handler.Control.Foreground = foregroundColor;
                handler.Control.Background = backgroundColor;
            });
            Eto.Style.Add<Eto.Wpf.Forms.Menu.MenuBarHandler>(null, handler =>
            {
                handler.Control.Foreground = foregroundColor;
                handler.Control.Background = backgroundColor;
            });
            Eto.Style.Add<Eto.Wpf.Forms.Menu.CheckMenuItemHandler>(null, handler =>
            {
                handler.Control.Foreground = foregroundColor;
                handler.Control.Background = backgroundColor;
            });
            Eto.Style.Add<Eto.Wpf.Forms.Controls.GridViewHandler>(null, handler =>
            {
            //handler.Control.Foreground = foregroundColor;
            handler.Control.Background = backgroundColor;
            handler.Control.RowBackground = backgroundColor;


            });



            Eto.Style.Add<Eto.Wpf.Forms.Controls.TreeGridViewHandler>(null, handler =>
            {
                handler.Control.Foreground = foregroundColor;
                handler.Control.Background = backgroundColor;
                handler.Control.RowBackground = backgroundColor;
                handler.Control.ColumnHeaderHeight = 0;

            });
            Eto.Style.Add<Eto.Wpf.Forms.Controls.TextBoxHandler>(null, handler =>
            {
                handler.Control.Foreground = foregroundColor;
                handler.Control.Background = backgroundColor;
                handler.Control.Resources.Add(System.Windows.SystemColors.WindowTextBrushKey, foregroundColor);
                handler.Control.Resources.Add(System.Windows.SystemColors.WindowBrushKey, backgroundColor);
            });

            Eto.Style.Add<Eto.Wpf.Forms.Controls.ComboBoxHandler>(null, handler =>
            {

                handler.Control.Foreground = foregroundColor;
                handler.Control.Background = backgroundColor;

            });

            Eto.Style.Add<Eto.Wpf.Forms.Controls.DropDownHandler>(null, handler =>
            {

                handler.Control.Foreground = foregroundColor;
                handler.Control.Background = backgroundColor;

                handler.Control.Resources.Add(System.Windows.SystemColors.WindowBrushKey, backgroundColor);
                handler.Control.Resources.Add(System.Windows.SystemColors.WindowColorKey, backgroundColor);



            });
            Eto.Style.Add<Eto.Wpf.Forms.Controls.ListBoxHandler>(null, handler =>
            {
                handler.Control.Foreground = foregroundColor;
                handler.Control.Background = backgroundColor;

            });
            Eto.Style.Add<Eto.Wpf.Forms.Controls.TabPageHandler>(null, handler =>
            {
                handler.Control.Foreground = foregroundColor;
                handler.Control.Background = backgroundColor;
                if (handler.Control.IsLoaded)
                {
                    handler.Control.Foreground = backgroundColor;
                    handler.Control.Background = foregroundColor;
                }
                //handler.BackgroundColor = Support.Themer.GetTheme().mainColor;

            });
            Eto.Style.Add<Eto.Wpf.Forms.Controls.TabControlHandler>(null, handler =>
            {
                handler.Control.Foreground = foregroundColor;
                handler.Control.Background = backgroundColor;
                //handler.BackgroundColor = Support.Themer.GetTheme().mainColor;

            });

            // https://stackoverflow.com/a/39348804/10434371
            ToolTipService.ShowOnDisabledProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(true));

            new Application(Eto.Platforms.Wpf).Run(new MainForm());
            
        }
    }
}
