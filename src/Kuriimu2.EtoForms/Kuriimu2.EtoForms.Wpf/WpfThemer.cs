using System.Windows;
using System.Windows.Controls;

namespace Kuriimu2.EtoForms.Wpf
{
    class WpfThemer
    {
        public static void LoadThemes()
        {
            System.Windows.Media.SolidColorBrush backgroundColor = new System.Windows.Media.SolidColorBrush(ConvertEtoColor(Support.Themer.Instance.GetTheme().MainColor));
            System.Windows.Media.SolidColorBrush foregroundColor = new System.Windows.Media.SolidColorBrush(ConvertEtoColor(Support.Themer.Instance.GetTheme().AltColor));
            System.Windows.Media.SolidColorBrush menuBarBackgroundColor = new System.Windows.Media.SolidColorBrush(ConvertEtoColor(Support.Themer.Instance.GetTheme().MenuBarBackColor));

            Eto.Style.Add<Eto.Wpf.Forms.Controls.PanelHandler>(null, panel =>
            {
                panel.BackgroundColor = Support.Themer.Instance.GetTheme().MainColor;
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
                handler.Control.Foreground = foregroundColor;

                var style = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
                style.Setters.Add(new Setter { Property = Control.BackgroundProperty, Value = new System.Windows.Media.LinearGradientBrush(backgroundColor.Color, ConvertEtoColor(Support.Themer.Instance.GetTheme().GridViewHeaderGradientColor), new Point(0, 0), new Point(0, 1)) });
                style.Setters.Add(new Setter { Property = Control.ForegroundProperty, Value = foregroundColor });
                style.Setters.Add(new Setter { Property = Control.BorderBrushProperty, Value = new System.Windows.Media.LinearGradientBrush(ConvertEtoColor(Support.Themer.Instance.GetTheme().GridViewHeaderBorderColor), ConvertEtoColor(Support.Themer.Instance.GetTheme().GridViewHeaderGradientColor), new Point(0, 1), new Point(0, 0)) });
                style.Setters.Add(new Setter { Property = Control.BorderThicknessProperty, Value = new Thickness(0, 0, 1, 1) });
                style.Setters.Add(new Setter { Property = Control.PaddingProperty, Value = new Thickness(4, 4, 4, 4) });

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
                {
                    if (handler.Control.TextBox != null)
                        handler.Control.TextBox.Style = textBoxStyle;
                };
                //Dropdown section
                handler.Control.Resources.Add(SystemColors.WindowBrushKey, backgroundColor);
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
                handler.Control.Background = new System.Windows.Media.SolidColorBrush(ConvertEtoColor(Support.Themer.Instance.GetTheme().ButtonBackColor));

                var style = new Style(typeof(Label));
                //Button's bg is diffrent when it is disabled(greyed out) therefore we have to change the text colour
                var triggerDisabled = new Trigger() { Property = Label.IsEnabledProperty, Value = false };
                triggerDisabled.Setters.Add(new Setter() { Property = Label.ForegroundProperty, Value = new System.Windows.Media.SolidColorBrush(ConvertEtoColor(Support.Themer.Instance.GetTheme().ButtonDisabledTextColor)) });
                style.Triggers.Add(triggerDisabled);
                style.Setters.Add(new Setter() { Property = Label.ForegroundProperty, Value = foregroundColor });
                //handler.Control.Foreground doesen't change text color,we have to use the label part
                handler.LabelPart.Style = style;
            });
            Eto.Style.Add<Eto.Wpf.Forms.Controls.TabPageHandler>(null, handler =>
            {
                handler.Control.Background = new System.Windows.Media.SolidColorBrush(ConvertEtoColor(Support.Themer.Instance.GetTheme().UnselectedTabBackColor));
                var style = new Style(typeof(TabItem));
                var setter = new Setter() { Property = TabItem.ForegroundProperty, Value = foregroundColor };
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
        private static System.Windows.Media.Color ConvertEtoColor(Eto.Drawing.Color color)
        {
            return System.Windows.Media.Color.FromArgb((byte)(color.A * 255.0), (byte)(color.R * 255.0), (byte)(color.G * 255.0), (byte)(color.B * 255.0));
        }
    }
}
