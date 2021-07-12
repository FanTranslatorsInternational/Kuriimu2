using Eto.Drawing;
using Eto.Forms;
using Kuriimu2.EtoForms.Resources;
using System;
using System.Collections.Generic;

namespace Kuriimu2.EtoForms.Support
{




    public class Themer
    {
        #region Localization Keys

        #endregion
        private static Dictionary<string, Theme> themeDict = new Dictionary<string, Theme>();

        public static void LoadThemes(bool firstTime)
        {
            if (firstTime)
            {
                themeDict.Add("light", new Theme(KnownColors.ThemeLight, KnownColors.ThemeDark, KnownColors.Black, KnownColors.NeonGreen, KnownColors.DarkRed,
                KnownColors.NeonGreen, KnownColors.Red, KnownColors.Orange, KnownColors.Wheat, Color.FromArgb(0xf0, 0xfd, 0xff), Color.FromArgb(0xcd, 0xf7, 0xfd), KnownColors.ControlLight, Color.FromArgb(240, 240, 240)
                ,Color.FromArgb(240, 240, 240),Color.FromArgb(230,230,230),KnownColors.Orange,KnownColors.LimeGreen,KnownColors.ControlDark,KnownColors.Control));

                themeDict.Add("dark", new Theme(KnownColors.ThemeDark, KnownColors.White, Color.FromArgb(90, 90, 90), KnownColors.NeonGreen, KnownColors.DarkRed,
                KnownColors.NeonGreen, KnownColors.Red, KnownColors.Orange, KnownColors.Wheat, KnownColors.DarkRed, KnownColors.DarkRed, KnownColors.ControlLight, Color.FromArgb(10, 10, 10)
                , Color.FromArgb(20, 20, 20), Color.FromArgb(10, 10, 10), KnownColors.Orange,KnownColors.LimeGreen, KnownColors.ControlDark,Color.FromArgb(60,60,60)
                ));

            }
            else
            {
                #region Styling

                var theme = GetTheme();


                Eto.Style.Add<Label>(null, text =>
                {
                    text.TextColor = theme.altColor;
                });
                Eto.Style.Add<Dialog>(null, dialog =>
                {
                    dialog.BackgroundColor = theme.mainColor;
                });
                Eto.Style.Add<CheckBox>(null, checkbox =>
                {
                    checkbox.BackgroundColor = theme.mainColor;
                    checkbox.TextColor = theme.altColor;
                });
                Eto.Style.Add<GroupBox>(null, groupBox =>
                {
                    groupBox.BackgroundColor = theme.mainColor;
                    groupBox.TextColor = theme.altColor;
                });


                #endregion



            }






        }





        public static void ChangeTheme(string theme, string ThemeRestartText, string ThemeRestartCaption
            , string ThemeUnsupportedPlatformText, string ThemeUnsupportedPlatformCaption)
        {
            if (Application.Instance.Platform.IsWpf)
            {

                Settings.Default.Theme = theme;
                Settings.Default.Save();

                MessageBox.Show(ThemeRestartText, ThemeRestartCaption);


            }
            else
            {
                MessageBox.Show(ThemeUnsupportedPlatformText, ThemeUnsupportedPlatformCaption);

            }

        }

        private static string Localize()
        {
            throw new NotImplementedException();
        }

        public static Theme GetTheme()
        {
            try
            {
                return themeDict[Settings.Default.Theme];
            }
            catch (KeyNotFoundException)
            {
                return themeDict["light"];
            }

        }


    }


}

public class Theme
{
    public Color mainColor { get; private set; }//Main background color
    public Color altColor { get; private set; }//text and foreground color
    public Color loggerBackColor { get; private set; }//Background of logger text areas
    public Color loggerTextColor { get; private set; }//Text of logger text areas
    public Color failColor { get; private set; }//fatal logger errors color
    public Color logInfoColor { get; private set; }//Info logger text color
    public Color logErrorColor { get; private set; }//Error logger text color
    public Color logWarningColor { get; private set; }//warning logger text color
    public Color logDefaultColor { get; private set; }//defualt logger text color
    public Color hexByteBack1Color { get; private set; } //every seccond byte in hex viewer
    public Color hexSidebarBackColor { get; private set; }//side bar color in hex viewer
    public Color controlColor { get; private set; }
    public Color menuBarBackColor { get; private set; }//Back colour of top menu bar
    public Color unselectedTabBackColor { get; private set; }//Background of unselected tab
    public Color windowBackColor { get; private set; } //Back of the main window, NOT the main panel
    public Color ArchiveChangedColor { get; private set; }//Archive viewer text color when a file is modifieed
    public Color progressColor { get; private set; } //Colour of the moving bar in a progress bar
    public Color progressBorderColor { get; private set; } //border color of progress bar
    public Color progressControlColor { get; private set; }//Background color of the progress bar
    public Theme(Color mainColor, Color altColor, Color loggerBackColor, Color loggerTextColor,
        Color failColor, Color logInfoColor, Color logErrorColor, Color logWarningColor, Color logDefaultColor,
        Color hexByteBack1Color, Color hexSidebarBackColor, Color controlColor, Color menuBarBackColor,
        Color unselectedTabBackColor, Color windowBackColor,Color ArchiveChangedColor,Color progressColor,Color progressBorderColor
        ,Color progressControlColor
        )
    {
        this.mainColor = mainColor;
        this.altColor = altColor;
        this.loggerBackColor = loggerBackColor;
        this.failColor = failColor;
        this.logInfoColor = logInfoColor;
        this.logErrorColor = logErrorColor;
        this.logWarningColor = logWarningColor;
        this.logDefaultColor = logDefaultColor;
        this.hexByteBack1Color = hexByteBack1Color;
        this.hexSidebarBackColor = hexSidebarBackColor;
        this.controlColor = controlColor;
        this.menuBarBackColor = menuBarBackColor;
        this.unselectedTabBackColor = unselectedTabBackColor;
        this.windowBackColor = windowBackColor;
        this.ArchiveChangedColor = ArchiveChangedColor;
        this.progressColor = progressColor;
        this.progressBorderColor = progressBorderColor;
        this.progressControlColor = progressControlColor;
    }



}