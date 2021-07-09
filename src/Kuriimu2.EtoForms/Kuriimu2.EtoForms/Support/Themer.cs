using Eto;
using Eto.Drawing;
using Eto.Forms;
using Kuriimu2.EtoForms.Resources;
using System.Collections.Generic;

namespace Kuriimu2.EtoForms.Support
{
    //Will apply to all




    public class Themer
    {
        private static Dictionary<string, Theme> themeDict = new Dictionary<string, Theme>();

        public static void LoadThemes()
        {
            Themer.themeDict.Add("dark", new Theme(Support.KnownColors.ThemeDark, Support.KnownColors.White));
            Themer.themeDict.Add("light", new Theme(Support.KnownColors.ThemeLight, Support.KnownColors.Black));
            //Add try and catch later
            var theme = GetTheme();
            #region Styling
            #region cross platform
            Eto.Style.Add<Panel>(null, panel =>
            {
                panel.BackgroundColor = theme.mainColor;
            });

            Eto.Style.Add<Label>(null, text =>
            {
                text.TextColor = theme.altColor;
            });
            Eto.Style.Add<Button>(null, button =>
            {
                button.BackgroundColor = theme.mainColor;
                button.TextColor = theme.altColor;
            });
            Eto.Style.Add<GridView>(null, gridview =>
            {
                gridview.BackgroundColor = Support.KnownColors.ThemeDark;

            });
            Eto.Style.Add<Dialog>(null, dialog =>
            {
                dialog.BackgroundColor = Support.KnownColors.ThemeDark;
            });
            Eto.Style.Add<CheckBox>(null, checkbox =>
            {
                checkbox.BackgroundColor = Support.KnownColors.ThemeDark;
                checkbox.TextColor = theme.altColor;
            });


            Eto.Style.Add<GroupBox>(null, groupBox =>
            {
                groupBox.BackgroundColor = Support.KnownColors.ThemeDark;
                groupBox.TextColor = theme.altColor;
            });


            Eto.Style.Add<TabControl>(null, tabPage =>
            {
                tabPage.BackgroundColor = theme.mainColor;
                //Padding = 0;
                //tabPage.TextColor = Support.KnownColors.White;
            });
            #endregion
            //WPF
            #region WPF
            //Cast like this beacuse WPF can't use eto colors
            //Alpha has to be 255 otherwise MenuBar doesen't work


            #endregion
            #endregion
        }



        public static Theme GetTheme()
        {
            return themeDict[Settings.Default.Theme];
        }


    }


}

public class Theme
{
    public Color mainColor { get; private set; }
    public Color altColor { get; private set; }
    public Theme(Color mainColorArgument, Color altColorArgument)
    {
        this.mainColor = mainColorArgument;
        this.altColor = altColorArgument;

    }


}