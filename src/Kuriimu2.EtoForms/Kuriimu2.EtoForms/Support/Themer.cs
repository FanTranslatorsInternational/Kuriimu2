using Eto.Drawing;
using Eto.Forms;
using Kuriimu2.EtoForms.Resources;
using System;
using System.Collections.Generic;

namespace Kuriimu2.EtoForms.Support
{
    public sealed class Themer
    {
        #region Localization Keys

        private const string ThemeRestartTextKey_ = "ThemeRestartText";
        private const string ThemeRestartCaptionKey_ = "ThemeRestartCaption";
        private const string ThemeUnsupportedPlatformTextKey_ = "ThemeUnsupportedPlatformText";
        private const string ThemeUnsupportedPlatformCaptionKey_ = "ThemeUnsupportedPlatformCaption";

        #endregion

        private readonly Lazy<Themer> lazy = new Lazy<Themer>(() => new Themer());
        public Themer Instance { get { return lazy.Value; } }
        private Dictionary<string, Theme> themeDict = new Dictionary<string, Theme>();
        private string currentThemeKey;
        private bool firstTime = true;

        public void LoadThemes()
        {
            if (firstTime)
            {
                currentThemeKey = Settings.Default.Theme;

                #region Themes

                #region light theme

                themeDict.Add("light", new Theme(
                mainColor: KnownColors.ThemeLight, altColor: KnownColors.Black, loggerBackColor: KnownColors.Black,
                loggerTextColor: KnownColors.NeonGreen, logFatalColor: KnownColors.DarkRed, logInfoColor: KnownColors.NeonGreen,
                logErrorColor: KnownColors.Red, logWarningColor: KnownColors.Orange,logDefaultColor: KnownColors.Wheat, hexByteBack1Color: Color.FromArgb(0xf0, 0xfd, 0xff),
                hexSidebarBackColor:Color.FromArgb(0xcd, 0xf7, 0xfd), controlColor: Color.FromArgb(0xf0, 0xfd, 0xff), menuBarBackColor: Color.FromArgb(245, 245, 245),
                unselectedTabBackColor: Color.FromArgb(238,238,238), windowBackColor: Color.FromArgb(240, 240, 240), archiveChangedColor: KnownColors.Orange,
                progressColor: KnownColors.LimeGreen, progressBorderColor: KnownColors.ControlDark, progressControlColor: KnownColors.Control,buttonBackColor:Color.FromArgb(221,221,221),
                buttonDisabledTextColor: KnownColors.Black, gridViewHeaderGradientColor: Color.FromArgb(243,243,243),gridViewHeaderBorderColor: Color.FromArgb(213,213,213),
                imageViewBackColor: KnownColors.DarkGreen));

                #endregion

                #region dark themes
                
                themeDict.Add("dark", new Theme(
                mainColor: KnownColors.ThemeDark, altColor: KnownColors.White, loggerBackColor: Color.FromArgb(90, 90, 90),
                loggerTextColor: KnownColors.NeonGreen, logFatalColor: KnownColors.DarkRed, logInfoColor: KnownColors.NeonGreen,
                logErrorColor: KnownColors.Red, logWarningColor: KnownColors.Orange,logDefaultColor: KnownColors.Wheat, hexByteBack1Color: KnownColors.DarkRed,
                hexSidebarBackColor: KnownColors.DarkRed, controlColor: Color.FromArgb(100, 100, 100), menuBarBackColor: Color.FromArgb(40, 40, 40),
                unselectedTabBackColor: Color.FromArgb(40, 40, 40), windowBackColor: Color.FromArgb(20, 20, 20), archiveChangedColor: KnownColors.Orange,
                progressColor: KnownColors.LimeGreen, progressBorderColor: KnownColors.ControlDark, progressControlColor: KnownColors.ControlDark,buttonBackColor:KnownColors.ThemeDark,
                buttonDisabledTextColor: Color.FromArgb(60, 60, 60), gridViewHeaderGradientColor:Color.FromArgb(12, 12, 12), gridViewHeaderBorderColor: Color.FromArgb(90,90,90),
                imageViewBackColor:KnownColors.DarkGreen));

                #endregion

                #endregion 

                if (!themeDict.ContainsKey(currentThemeKey))
                {
                    currentThemeKey = "light";
                }
                firstTime = false;
                return;
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
        public void ChangeTheme(string theme)
        {
            if (Application.Instance.Platform.IsWpf)
            {
                Settings.Default.Theme = theme;
                Settings.Default.Save();
                MessageBox.Show(Application.Instance.Localize(this,ThemeRestartTextKey_), Application.Instance.Localize(this,ThemeRestartCaptionKey_));
            }
            else
            {
                MessageBox.Show(Application.Instance.Localize(this,ThemeUnsupportedPlatformTextKey_), Application.Instance.Localize(this,ThemeUnsupportedPlatformCaptionKey_));
            }
        }
        public Theme GetTheme()
        {
            return themeDict[currentThemeKey];
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
    public Color HexByteBack1Color {get;} //every second byte in hex viewer
    public Color HexSidebarBackColor {get;}//side bar color in hex viewer
    public Color ControlColor {get;}
    public Color MenuBarBackColor {get;}//Back colour of top menu bar
    public Color UnselectedTabBackColor {get;}//Background of unselected tab
    public Color WindowBackColor {get;} //Back of the main window, NOT the main panel
    public Color ArchiveChangedColor {get;}//Archive viewer text color when a file is modified
    public Color ProgressColor {get;} //Colour of the moving bar in a progress bar
    public Color ProgressBorderColor {get;} //border color of progress bar
    public Color ProgressControlColor {get;}//Background color of the progress bar
    public Color ButtonBackColor { get; } //Background colour of a button
    public Color ButtonDisabledTextColor {get;} //Text colour of a greyedout/disabledbutton
    public Color GridViewHeaderGradientColor {get;} //Graident END color of gridview header
    public Color GridViewHeaderBorderColor {get;} //Border of grid view header
    public Color ImageViewBackColor {get;} //Background of image viewer
    public Theme(Color mainColor, Color altColor, Color loggerBackColor, Color loggerTextColor,
        Color logFatalColor, Color logInfoColor, Color logErrorColor, Color logWarningColor, Color logDefaultColor,
        Color hexByteBack1Color, Color hexSidebarBackColor, Color controlColor, Color menuBarBackColor,
        Color unselectedTabBackColor, Color windowBackColor,Color archiveChangedColor,Color progressColor,Color progressBorderColor,
        Color progressControlColor,Color buttonDisabledTextColor,Color buttonBackColor,Color gridViewHeaderGradientColor,Color gridViewHeaderBorderColor,
        Color imageViewBackColor)
    {
        MainColor = mainColor;
        AltColor = altColor;
        LoggerBackColor = loggerBackColor;
        LoggerTextColor = loggerTextColor;
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
        ButtonBackColor = buttonBackColor;
        ButtonDisabledTextColor = buttonDisabledTextColor;
        GridViewHeaderGradientColor = gridViewHeaderGradientColor;
        GridViewHeaderBorderColor = gridViewHeaderBorderColor;
        ImageViewBackColor = imageViewBackColor;
    }
}