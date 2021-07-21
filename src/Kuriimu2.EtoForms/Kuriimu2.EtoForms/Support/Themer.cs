using Eto.Drawing;
using Eto.Forms;
using Kuriimu2.EtoForms.Resources;
using System;
using System.Collections.Generic;

namespace Kuriimu2.EtoForms.Support
{
    public class Themer
    {
        private static Dictionary<string, Theme> themeDict = new Dictionary<string, Theme>();
        public static void LoadThemes(bool firstTime)
        {
            if (firstTime)
            {
                #region Themes

                #region light theme

                themeDict.Add("light", new Theme(
                mainColor: KnownColors.ThemeLight, altColor: KnownColors.Black, loggerBackColor: KnownColors.Black,
                loggerTextColor: KnownColors.NeonGreen, logFatalColor: KnownColors.DarkRed, logInfoColor: KnownColors.NeonGreen,
                logErrorColor: KnownColors.Red, logWarningColor: KnownColors.Orange,logDefaultColor: KnownColors.Wheat, hexByteBack1Color: Color.FromArgb(0xf0, 0xfd, 0xff),
                hexSidebarBackColor:Color.FromArgb(0xcd, 0xf7, 0xfd), controlColor: Color.FromArgb(0xf0, 0xfd, 0xff), menuBarBackColor: Color.FromArgb(245, 245, 245),
                unselectedTabBackColor: KnownColors.ControlLight, windowBackColor: Color.FromArgb(240, 240, 240), archiveChangedColor: KnownColors.Orange,
                progressColor: KnownColors.LimeGreen, progressBorderColor: KnownColors.ControlDark, progressControlColor: KnownColors.Control,
                buttonDisabledTextColor: KnownColors.Black));

                #endregion

                #region dark themes

                themeDict.Add("dark", new Theme(
                mainColor: KnownColors.ThemeDark, altColor: KnownColors.White, loggerBackColor: Color.FromArgb(90, 90, 90),
                loggerTextColor: KnownColors.NeonGreen, logFatalColor: KnownColors.DarkRed, logInfoColor: KnownColors.NeonGreen,
                logErrorColor: KnownColors.Red, logWarningColor: KnownColors.Orange,logDefaultColor: KnownColors.Wheat, hexByteBack1Color: KnownColors.DarkRed,
                hexSidebarBackColor: KnownColors.DarkRed, controlColor: Color.FromArgb(100, 100, 100), menuBarBackColor: Color.FromArgb(40, 40, 40),
                unselectedTabBackColor: Color.FromArgb(40, 40, 40), windowBackColor: Color.FromArgb(20, 20, 20), archiveChangedColor: KnownColors.Orange,
                progressColor: KnownColors.LimeGreen, progressBorderColor: KnownColors.ControlDark, progressControlColor: KnownColors.ControlDark,
                buttonDisabledTextColor: Color.FromArgb(60, 60, 60)));

                #endregion

                #endregion 
            }
            else
            {
                #region Styling

                var theme = GetTheme();
                Eto.Style.Add<Label>(null, text =>
                {
                    text.TextColor = theme.AltColor;
                });
                Eto.Style.Add<Dialog>(null, dialog =>
                {
                    dialog.BackgroundColor = theme.MainColor;
                });
                Eto.Style.Add<CheckBox>(null, checkbox =>
                {
                    checkbox.BackgroundColor = theme.MainColor;
                    checkbox.TextColor = theme.AltColor;
                });
                Eto.Style.Add<GroupBox>(null, groupBox =>
                {
                    groupBox.BackgroundColor = theme.MainColor;
                    groupBox.TextColor = theme.AltColor;
                });

                #endregion
            }
        }
        public static void ChangeTheme(string theme, string ThemeRestartText, string ThemeRestartCaption,
             string ThemeUnsupportedPlatformText, string ThemeUnsupportedPlatformCaption)
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
        public static Theme GetTheme()
        {
            if (themeDict.ContainsKey(Settings.Default.Theme))
            {
                return themeDict[Settings.Default.Theme];
            }
            else
            {
                return themeDict["light"];
            }
        }
    }
}
public class Theme
{
    public Color MainColor {get;}//Main background color
    public Color AltColor {get;}//text and foreground color
    public Color LoggerBackColor {get;}//Background of logger text areas
    public Color LoggerTextColor {get;}//Text of logger text areas
    public Color LogFatalColor {get;}//fatal logger errors color
    public Color LogInfoColor {get;}//Info logger text color
    public Color LogErrorColor {get;}//Error logger text color
    public Color LogWarningColor {get;}//warning logger text color
    public Color LogDefaultColor {get;}//defualt logger text color
    public Color HexByteBack1Color {get;} //every seccond byte in hex viewer
    public Color HexSidebarBackColor {get;}//side bar color in hex viewer
    public Color ControlColor {get;}
    public Color MenuBarBackColor {get;}//Back colour of top menu bar
    public Color UnselectedTabBackColor {get;}//Background of unselected tab
    public Color WindowBackColor {get;} //Back of the main window, NOT the main panel
    public Color ArchiveChangedColor {get;}//Archive viewer text color when a file is modifieed
    public Color ProgressColor {get;} //Colour of the moving bar in a progress bar
    public Color ProgressBorderColor {get;} //border color of progress bar
    public Color ProgressControlColor {get;}//Background color of the progress bar
    public Color ButtonDisabledTextColor {get;} //Text colour of a greyedout/disabledbutton
    public Theme(Color mainColor, Color altColor, Color loggerBackColor, Color loggerTextColor,
        Color logFatalColor, Color logInfoColor, Color logErrorColor, Color logWarningColor, Color logDefaultColor,
        Color hexByteBack1Color, Color hexSidebarBackColor, Color controlColor, Color menuBarBackColor,
        Color unselectedTabBackColor, Color windowBackColor,Color archiveChangedColor,Color progressColor,Color progressBorderColor
        ,Color progressControlColor,Color buttonDisabledTextColor
        )
    {
        MainColor = mainColor;
        AltColor = altColor;
        LoggerBackColor = loggerBackColor;
        LogFatalColor = logFatalColor;
        LogInfoColor = logInfoColor;
        LogErrorColor = logErrorColor;
        LogWarningColor = logWarningColor;
        LogDefaultColor = logDefaultColor;
        HexByteBack1Color = hexByteBack1Color;
        HexSidebarBackColor = hexSidebarBackColor;
        ControlColor = controlColor;
        MenuBarBackColor = menuBarBackColor;
        UnselectedTabBackColor = unselectedTabBackColor;
        WindowBackColor = windowBackColor;
        ArchiveChangedColor = archiveChangedColor;
        ProgressColor = progressColor;
        ProgressBorderColor = progressBorderColor;
        ProgressControlColor = progressControlColor;
        ButtonDisabledTextColor = buttonDisabledTextColor;
    }
}