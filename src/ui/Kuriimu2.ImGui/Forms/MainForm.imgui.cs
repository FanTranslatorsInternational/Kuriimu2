using System.Collections.Generic;
using System.Numerics;
using ImGui.Forms;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Menu;
using ImGui.Forms.Models;
using ImGuiNET;
using Kuriimu2.ImGui.Components;
using Kuriimu2.ImGui.Resources;
using Veldrid;

namespace Kuriimu2.ImGui.Forms
{
    partial class MainForm
    {
        private MenuBarButton _openButton;
        private MenuBarButton _openWithButton;
        private MenuBarButton _saveAllButton;

        private MenuBarButton _imageTranscoderButton;
        private MenuBarButton _rawImageViewerButton;

        private MenuBarButton _batchExtractButton;
        private MenuBarButton _batchInjectButton;
        private MenuBarButton _textSequencerButton;
        private MenuBarButton _hashesButton;

        private MenuBarButton _ciphersButton;
        private MenuBarButton _compressionsButton;

        private MenuBarCheckBox _includeDevBuildsButton;
        private MenuBarRadio _changeLanguageMenu;
        private MenuBarRadio _changeThemeMenu;

        private MenuBarButton _pluginsButton;
        private MenuBarButton _aboutButton;

        private TabControl _tabControl;
        private ProgressBar _progressBar;
        private StatusLabel _statusText;

        private IDictionary<MenuBarCheckBox, string> _localeItems = new Dictionary<MenuBarCheckBox, string>();
        private IDictionary<MenuBarCheckBox, Theme> _themes = new Dictionary<MenuBarCheckBox, Theme>();

        private void InitializeComponent()
        {
            #region Controls

            _openButton = new MenuBarButton
            {
                Text = LocalizationResources.MenuFileOpen,
                KeyAction = new(ModifierKeys.Control, Key.O, LocalizationResources.MenuFileOpenShortcut)
            };
            _openWithButton = new MenuBarButton
            {
                Text = LocalizationResources.MenuFileOpenWith,
                KeyAction = new(ModifierKeys.Control | ModifierKeys.Shift, Key.O, LocalizationResources.MenuFileOpenWithShortcut)
            };
            _saveAllButton = new MenuBarButton
            {
                Text = LocalizationResources.MenuFileSaveAll,
                Enabled = false,
                KeyAction = new(ModifierKeys.Control | ModifierKeys.Shift, Key.S, LocalizationResources.MenuFileSaveAllShortcut)
            };

            _imageTranscoderButton = new MenuBarButton
            {
                Text = LocalizationResources.MenuToolsImageTranscoder
            };
            _rawImageViewerButton = new MenuBarButton
            {
                Text = LocalizationResources.MenuToolsRawImageViewer
            };

            _batchExtractButton = new MenuBarButton { Text = LocalizationResources.MenuToolsBatchExtractor };
            _batchInjectButton = new MenuBarButton { Text = LocalizationResources.MenuToolsBatchInjector };
            _textSequencerButton = new MenuBarButton { Text = LocalizationResources.MenuToolsTextSequenceSearcher };
            _hashesButton = new MenuBarButton { Text = LocalizationResources.MenuToolsHashes };
            _rawImageViewerButton = new MenuBarButton { Text = LocalizationResources.MenuToolsRawImageViewer };

            _ciphersButton = new MenuBarButton { Text = LocalizationResources.MenuToolsCiphers };
            _compressionsButton = new MenuBarButton { Text = LocalizationResources.MenuToolsCompressions };

            _includeDevBuildsButton = new MenuBarCheckBox
            {
                Text = LocalizationResources.MenuSettingsIncludeDevBuilds,
                Checked = SettingsResources.IncludeDevBuilds
            };
            _changeLanguageMenu = new MenuBarRadio { Text = LocalizationResources.MenuSettingsChangeLanguage };
            _changeThemeMenu = new MenuBarRadio { Text = LocalizationResources.MenuSettingsChangeTheme };

            _pluginsButton = new MenuBarButton { Text = LocalizationResources.MenuPluginsTitle };
            _aboutButton = new MenuBarButton { Text = LocalizationResources.MenuAboutTitle };

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
                            new MenuBarSplitter(),
                            _imageTranscoderButton,
                            _rawImageViewerButton
                        }
                    },
                    //new MenuBarMenu{Text = LocalizationResources.MenuTools, Items =
                    //{
                    //    _batchExtractButton,
                    //    _batchInjectButton,
                    //    _textSequencerButton,
                    //    _hashesButton,
                    //    _rawImageViewerButton
                    //}},
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
