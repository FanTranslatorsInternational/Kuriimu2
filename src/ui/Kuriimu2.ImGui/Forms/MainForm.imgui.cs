using Hexa.NET.ImGui;
using ImGui.Forms;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Menu;
using ImGui.Forms.Models;
using Kuriimu2.ImGui.Components;
using Kuriimu2.ImGui.Resources;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using ImGui.Forms.Models.IO;

namespace Kuriimu2.ImGui.Forms
{
    internal partial class MainForm
    {
        private MenuBarButton _openButton;
        private MenuBarButton _openWithButton;
        private MenuBarButton _saveAllButton;

        private MenuBarButton _ciphersButton;
        private MenuBarButton _compressionsButton;
        private MenuBarButton _checksumsButton;

        private MenuBarButton _imageTranscoderButton;
        private MenuBarButton _rawImageViewerButton;

        private MenuBarButton _textSequencerButton;

        private MenuBarButton _batchButton;

        private MenuBarCheckBox _includeDevBuildsButton;
        private MenuBarRadio _changeLanguageMenu;
        private MenuBarRadio _changeThemeMenu;

        private MenuBarButton _pluginsButton;
        private MenuBarButton _preferencesButton;
        private MenuBarButton _aboutButton;

        private TabControl _tabControl;
        private ProgressBar _progressBar;
        private StatusLabel _statusText;

        private readonly Dictionary<MenuBarCheckBox, string> _localeItems = [];
        private readonly Dictionary<MenuBarCheckBox, Theme> _themes = [];

        [MemberNotNull(nameof(_openButton), nameof(_openWithButton), nameof(_saveAllButton))]
        [MemberNotNull(nameof(_ciphersButton), nameof(_compressionsButton), nameof(_checksumsButton))]
        [MemberNotNull(nameof(_imageTranscoderButton), nameof(_rawImageViewerButton))]
        [MemberNotNull(nameof(_textSequencerButton), nameof(_batchButton))]
        [MemberNotNull(nameof(_includeDevBuildsButton), nameof(_changeLanguageMenu), nameof(_changeThemeMenu))]
        [MemberNotNull(nameof(_pluginsButton), nameof(_preferencesButton), nameof(_aboutButton))]
        [MemberNotNull(nameof(_tabControl), nameof(_progressBar), nameof(_statusText))]
        private void InitializeComponent()
        {
            #region Controls

            _openButton = new MenuBarButton
            {
                Text = LocalizationResources.MenuFileOpen,
                KeyAction = new KeyCommand(ImGuiKey.ModCtrl, ImGuiKey.O, LocalizationResources.MenuFileOpenShortcut)
            };
            _openWithButton = new MenuBarButton
            {
                Text = LocalizationResources.MenuFileOpenWith,
                KeyAction = new KeyCommand(ImGuiKey.ModCtrl | ImGuiKey.ModShift, ImGuiKey.O, LocalizationResources.MenuFileOpenWithShortcut)
            };
            _saveAllButton = new MenuBarButton
            {
                Text = LocalizationResources.MenuFileSaveAll,
                Enabled = false,
                KeyAction = new KeyCommand(ImGuiKey.ModCtrl | ImGuiKey.ModShift, ImGuiKey.S, LocalizationResources.MenuFileSaveAllShortcut)
            };

            _imageTranscoderButton = new MenuBarButton { Text = LocalizationResources.MenuToolsImageTranscoder };
            _rawImageViewerButton = new MenuBarButton { Text = LocalizationResources.MenuToolsRawImageViewer };

            _textSequencerButton = new MenuBarButton { Text = LocalizationResources.MenuToolsTextSequenceSearcher };
            _batchButton = new MenuBarButton { Text = LocalizationResources.MenuToolsBatch };

            _ciphersButton = new MenuBarButton { Text = LocalizationResources.MenuToolsCiphers };
            _compressionsButton = new MenuBarButton { Text = LocalizationResources.MenuToolsCompressions };
            _checksumsButton = new MenuBarButton { Text = LocalizationResources.MenuToolsChecksums };

            _includeDevBuildsButton = new MenuBarCheckBox
            {
                Text = LocalizationResources.MenuSettingsIncludeDevBuilds,
                Checked = SettingsResources.IncludeDevBuilds
            };
            _changeLanguageMenu = new MenuBarRadio { Text = LocalizationResources.MenuSettingsChangeLanguage };
            _changeThemeMenu = new MenuBarRadio { Text = LocalizationResources.MenuSettingsChangeTheme };

            _pluginsButton = new MenuBarButton { Text = LocalizationResources.MenuHelpPluginsInstalled };
            _preferencesButton = new MenuBarButton { Text = LocalizationResources.MenuHelpPreferences };
            _aboutButton = new MenuBarButton { Text = LocalizationResources.MenuHelpAbout };

            AddLanguages(_changeLanguageMenu);
            AddThemes(_changeThemeMenu);

            #region Main menu bar

            var mainMenuBar = new MainMenuBar
            {
                Items =
                {
                    new MenuBarMenu
                    {
                        Text = LocalizationResources.MenuFile,
                        Items =
                        {
                            _openButton,
                            _openWithButton,
                            new MenuBarSplitter(),
                            _saveAllButton
                        }
                    },
                    new MenuBarMenu
                    {
                        Text = LocalizationResources.MenuTools,
                        Items =
                        {
                            _ciphersButton,
                            _compressionsButton,
                            _checksumsButton,
                            new MenuBarSplitter(),
                            _imageTranscoderButton,
                            _rawImageViewerButton,
                            new MenuBarSplitter(),
                            _textSequencerButton,
                            new MenuBarSplitter(),
                            _batchButton
                        }
                    },
                    new MenuBarMenu
                    {
                        Text = LocalizationResources.MenuSettings, Items =
                        {
                            _includeDevBuildsButton,
                            _changeLanguageMenu,
                            _changeThemeMenu
                        }
                    },
                    new MenuBarMenu
                    {
                        Text = LocalizationResources.MenuHelp, Items =
                        {
                            _pluginsButton,
                            _preferencesButton,
                            new MenuBarSplitter(),
                            _aboutButton
                        }
                    }
                }
            };

            #endregion

            #region Main Content

            _tabControl = new TabControl();
            _progressBar = new ProgressBar { Size = new Size(.3f, 24), ProgressColor = ColorResources.Progress };
            _statusText = new StatusLabel { Width = SizeValue.Relative(.5f) };

            var mainLayout = new StackLayout
            {
                Alignment = Alignment.Vertical,
                ItemSpacing = 4,
                Items =
                {
                    _tabControl,
                    new StackLayout
                    {
                        Size = new Size(SizeValue.Parent, 24),
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

            Icon = ImageResources.Icon;
            AllowDragDrop = true;

            Size = new Vector2(1200, 700);
            Style.SetStyle(ImGuiStyleVar.WindowPadding, new Vector2(4));

            MenuBar = mainMenuBar;
            Content = mainLayout;

            #endregion
        }

        private void AddLanguages(MenuBarRadio menu)
        {
            menu.CheckItems.Clear();

            foreach (string locale in LocalizationResources.Instance.GetLocales())
            {
                var checkBox = new MenuBarCheckBox
                {
                    Text = LocalizationResources.Instance.GetLanguageName(locale),
                    Checked = SettingsResources.Locale == locale
                };

                _localeItems[checkBox] = locale;

                menu.CheckItems.Add(checkBox);
            }
        }

        private void AddThemes(MenuBarRadio menu)
        {
            var lightCheckBox = new MenuBarCheckBox { Text = LocalizationResources.MenuSettingsChangeThemeLight, Checked = SettingsResources.Theme == Theme.Light };
            var darkCheckBox = new MenuBarCheckBox { Text = LocalizationResources.MenuSettingsChangeThemeDark, Checked = SettingsResources.Theme == Theme.Dark };

            _themes.Clear();
            _themes[lightCheckBox] = Theme.Light;
            _themes[darkCheckBox] = Theme.Dark;

            menu.CheckItems.Clear();
            menu.CheckItems.Add(lightCheckBox);
            menu.CheckItems.Add(darkCheckBox);
        }
    }
}
