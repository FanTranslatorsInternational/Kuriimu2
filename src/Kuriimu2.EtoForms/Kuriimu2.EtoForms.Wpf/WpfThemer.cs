using System.Windows;
using System.Windows.Controls;

namespace Kuriimu2.EtoForms.Wpf
{
    class WpfThemer
    {
        private static System.Windows.Media.Color ConvertEtoColor(Eto.Drawing.Color color)
        {

            return System.Windows.Media.Color.FromArgb((byte)(color.A * 255.0), (byte)(color.R * 255.0), (byte)(color.G * 255.0), (byte)(color.B * 255.0));
        }
        public static void LoadThemesWpf()
        {
            System.Windows.Media.SolidColorBrush backgroundColor = new System.Windows.Media.SolidColorBrush(ConvertEtoColor(Support.Themer.GetTheme().mainColor));
            System.Windows.Media.SolidColorBrush foregroundColor = new System.Windows.Media.SolidColorBrush(ConvertEtoColor(Support.Themer.GetTheme().altColor));
            System.Windows.Media.SolidColorBrush menuBarBackgroundColor = new System.Windows.Media.SolidColorBrush(ConvertEtoColor(Support.Themer.GetTheme().menuBarBackColor));

            Eto.Style.Add<Eto.Wpf.Forms.Controls.PanelHandler>(null, panel =>
            {
                panel.BackgroundColor = Support.Themer.GetTheme().mainColor;
 

            });

            Eto.Style.Add<Eto.Wpf.Forms.Menu.ButtonMenuItemHandler>(null, handler =>
            {
                handler.Control.Foreground = foregroundColor;
                handler.Control.Background = menuBarBackgroundColor;
            });
            Eto.Style.Add<Eto.Wpf.Forms.Menu.MenuBarHandler>(null, handler =>
            {
                handler.Control.Foreground = foregroundColor;
                handler.Control.Background = menuBarBackgroundColor;
            });
            Eto.Style.Add<Eto.Wpf.Forms.Menu.CheckMenuItemHandler>(null, handler =>
            {
                handler.Control.Foreground = foregroundColor;
                handler.Control.Background = menuBarBackgroundColor;

            });
            Eto.Style.Add<Eto.Wpf.Forms.Controls.GridViewHandler>(null, handler =>
            {
                handler.Control.Background = backgroundColor;
                handler.Control.RowBackground = backgroundColor;

                Style style = new Style(typeof(System.Windows.Controls.Primitives
               .DataGridColumnHeader));

                style.Setters.Add(new Setter { Property = Control.BackgroundProperty, Value = backgroundColor});
                style.Setters.Add(new Setter { Property = Control.ForegroundProperty, Value = foregroundColor });


                style.Setters.Add(new Setter { Property = Control.BorderBrushProperty, Value = foregroundColor});
                style.Setters.Add(new Setter { Property = Control.BorderThicknessProperty, Value = new Thickness(0, 0, 1, 0) });

                handler.Control.ColumnHeaderStyle = style;


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
            });
            Eto.Style.Add<Eto.Wpf.Forms.Controls.DropDownHandler>(null, handler =>
            {
                handler.Control.Foreground = foregroundColor;
                handler.Control.Background = backgroundColor;

            });
            Eto.Style.Add<Eto.Wpf.Forms.Controls.ComboBoxHandler>(null, handler =>
            {
                //Textbox section
                var textBoxStyle = new Style(typeof(TextBox));
                textBoxStyle.Setters.Add(new Setter() { Property = TextBox.BackgroundProperty, Value = backgroundColor });
                textBoxStyle.Setters.Add(new Setter() { Property = TextBox.ForegroundProperty, Value = foregroundColor });


                handler.Control.Loaded += (sender, e) =>
                {//Makes this only execute after it has initialized so Textbox won't return null

                    handler.Control.TextBox.Style = textBoxStyle;
                };
                //Dropdown section
                handler.Control.Resources.Add(System.Windows.SystemColors.WindowBrushKey, backgroundColor);
                handler.Control.Foreground = foregroundColor;
                handler.Control.Background = backgroundColor;

            });
            Eto.Style.Add<Eto.Wpf.Forms.Controls.ListBoxHandler>(null, handler =>
            {
                handler.Control.Foreground = foregroundColor;
                handler.Control.Background = backgroundColor;
            });
            
            Eto.Style.Add<Eto.Wpf.Forms.Controls.ButtonHandler>(null, handler =>
            {
                handler.Control.Background = backgroundColor;
                handler.TextColor = Support.Themer.GetTheme().altColor;
            });


            Eto.Style.Add<Eto.Wpf.Forms.Controls.TabPageHandler>(null, handler =>
            {

                handler.Control.Background = new System.Windows.Media.SolidColorBrush(ConvertEtoColor(Support.Themer.GetTheme().unselectedTabBackColor));

                var style = new Style(typeof(TabItem));
                Setter setter = new Setter() { Property = TabItem.ForegroundProperty, Value = foregroundColor };
                var triggerSelected = new Trigger() { Property = TabItem.IsSelectedProperty, Value = false }; triggerSelected.Setters.Add(setter);
                style.Triggers.Add(triggerSelected);
                handler.Control.Style = style;

            });

            Eto.Style.Add<Eto.Wpf.Forms.Controls.TabControlHandler>(null, handler =>
            {
                handler.Control.Foreground = foregroundColor;
                handler.Control.Background = backgroundColor;
          

            });


        }

    }
}
