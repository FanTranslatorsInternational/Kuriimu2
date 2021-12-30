using System;
using System.Numerics;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Menu;
using ImGui.Forms.Models;
using Kuriimu2.ImGui.Resources;

namespace Kuriimu2.ImGui.Forms
{
    partial class MainForm
    {
        private MenuBarButton _openButton;
        private MenuBarButton _openWithButton;
        private MenuBarButton _saveAllButton;

        private MenuBarButton _batchExtractButton;
        private MenuBarButton _batchInjectButton;
        private MenuBarButton _textSequencerButton;
        private MenuBarButton _hashesButton;
        private MenuBarButton _rawImageViewerButton;

        private MenuBarButton _encryptButton;
        private MenuBarButton _decryptButton;

        private MenuBarButton _compressButton;
        private MenuBarButton _decompressButton;

        private MenuBarCheckBox _includeDevBuildsButton;
        private MenuBarRadio _changeLanguageMenu;
        private MenuBarRadio _changeThemeMenu;

        private TabControl _tabControl;
        private ProgressBar _progressBar;
        private Label _statusText;

        private void InitializeComponent()
        {
            #region Setting application

            if (Enum.TryParse<Theme>(Settings.Default.Theme, out var parsedTheme))
                Theme = parsedTheme;
            LocalizationResources.Instance.ChangeLocale(Settings.Default.Locale);

            #endregion

            #region Controls

            _openButton = new MenuBarButton { Caption = LocalizationResources.OpenResource() };
            _openWithButton = new MenuBarButton { Caption = LocalizationResources.OpenWithResource() };
            _saveAllButton = new MenuBarButton { Caption = LocalizationResources.SaveAllResource(), Enabled = false };

            _batchExtractButton = new MenuBarButton { Caption = LocalizationResources.BatchExtractorResource() };
            _batchInjectButton = new MenuBarButton { Caption = LocalizationResources.BatchInjectorResource() };
            _textSequencerButton = new MenuBarButton { Caption = LocalizationResources.TextSequenceSearcherResource() };
            _hashesButton = new MenuBarButton { Caption = LocalizationResources.HashesResource() };
            _rawImageViewerButton = new MenuBarButton { Caption = LocalizationResources.RawImageViewerResource() };

            _encryptButton = new MenuBarButton { Caption = LocalizationResources.EncryptResource() };
            _decryptButton = new MenuBarButton { Caption = LocalizationResources.DecryptResource() };

            _compressButton = new MenuBarButton { Caption = LocalizationResources.CompressResource() };
            _decompressButton = new MenuBarButton { Caption = LocalizationResources.DecompressResource() };

            _includeDevBuildsButton = new MenuBarCheckBox
            {
                Caption = LocalizationResources.IncludeDevBuildsResource(),
                Checked = Settings.Default.IncludeDevBuilds
            };
            _changeLanguageMenu = new MenuBarRadio { Caption = LocalizationResources.ChangeLanguageResource() };
            _changeThemeMenu = new MenuBarRadio { Caption = LocalizationResources.ChangeThemeResource() };

            AddLanguages(_changeLanguageMenu);
            AddThemes(_changeThemeMenu);

            #region Main menu bar

            var mainMenuBar = new MainMenuBar
            {
                Items =
                {
                    new MenuBarMenu{Caption = LocalizationResources.FileResource(), Items =
                    {
                        _openButton,
                        _openWithButton,
                        new MenuBarSplitter(),
                        _saveAllButton
                    }},
                    new MenuBarMenu{Caption = LocalizationResources.ToolsResource(), Items =
                    {
                        _batchExtractButton,
                        _batchInjectButton,
                        _textSequencerButton,
                        _hashesButton,
                        _rawImageViewerButton
                    }},
                    new MenuBarMenu{Caption = LocalizationResources.CiphersResource(), Items =
                    {
                        _encryptButton,
                        _decryptButton
                    }},
                    new MenuBarMenu{Caption = LocalizationResources.CompressionsResource(), Items =
                    {
                        _compressButton,
                        _decompressButton
                    }},
                    new MenuBarMenu{Caption = LocalizationResources.SettingsResource(), Items =
                    {
                        _includeDevBuildsButton,
                        _changeLanguageMenu,
                        _changeThemeMenu
                    }}
                }
            };

            #endregion

            #region Main Content

            _tabControl = new TabControl();
            _progressBar = new ProgressBar { Size = new Size(.5f, 24) };
            _statusText = new Label();

            var mainLayout = new StackLayout
            {
                Alignment = Alignment.Vertical,
                ItemSpacing = 4,
                Items =
                {
                    _tabControl,
                    new StackLayout
                    {
                        Size = new Size(1f, 24),
                        Alignment = Alignment.Horizontal,
                        ItemSpacing = 4,
                        Items =
                        {
                            _progressBar,
                            new StackItem(_statusText) {VerticalAlignment = VerticalAlignment.Center}
                        }
                    }
                }
            };

            #endregion

            #endregion

            #region Properties

            DefaultFont = Fonts.Arial(15);

            Title = "Kuriimu2";
            Icon = ImageResources.Icon;

            Size = new Vector2(1116, 643);
            Padding = new Vector2(4);

            MainMenuBar = mainMenuBar;
            Content = mainLayout;

            #endregion
        }

        private void AddLanguages(MenuBarRadio menu)
        {
            foreach (var locale in LocalizationResources.GetLocales())
            {
                menu.CheckItems.Add(new MenuBarCheckBox
                {
                    Caption = LocalizationResources.GetLanguageName(locale),
                    Checked = LocalizationResources.Instance.CurrentLocale == locale
                });
            }
        }

        private void AddThemes(MenuBarRadio menu)
        {
            var lightCheckBox = new MenuBarCheckBox { Caption = LocalizationResources.ThemeLightResource(), Checked = Theme == Theme.Light };
            var darkCheckBox = new MenuBarCheckBox { Caption = LocalizationResources.ThemeDarkResource(), Checked = Theme == Theme.Dark };

            menu.CheckItems.Add(lightCheckBox);
            menu.CheckItems.Add(darkCheckBox);
        }
    }
}
