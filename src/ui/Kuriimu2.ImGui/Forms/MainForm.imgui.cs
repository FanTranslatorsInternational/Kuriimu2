using System.Collections.Generic;
using System.Numerics;
using ImGui.Forms;
using ImGui.Forms.Controls;
using ImGui.Forms.Controls.Layouts;
using ImGui.Forms.Controls.Menu;
using ImGui.Forms.Models;
using ImGui.Forms.Resources;
using ImGuiNET;
using Kuriimu2.ImGui.Controls;
using Kuriimu2.ImGui.Resources;
using ImageResources = Kuriimu2.ImGui.Resources.ImageResources;

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

        private MenuBarButton _aboutButton;

        private TabControl _tabControl;
        private ProgressBar _progressBar;
        private StatusLabel _statusText;

        private IDictionary<MenuBarCheckBox, Theme> _themes = new Dictionary<MenuBarCheckBox, Theme>();

        private void InitializeComponent()
        {
            #region Controls

            _openButton = new MenuBarButton { Text = LocalizationResources.MenuFileOpen() };
            _openWithButton = new MenuBarButton { Text = LocalizationResources.MenuFileOpenWith() };
            _saveAllButton = new MenuBarButton { Text = LocalizationResources.MenuFileSaveAll(), Enabled = false };

            _batchExtractButton = new MenuBarButton { Text = LocalizationResources.MenuToolsBatchExtractor() };
            _batchInjectButton = new MenuBarButton { Text = LocalizationResources.MenuToolsBatchInjector() };
            _textSequencerButton = new MenuBarButton { Text = LocalizationResources.MenuToolsTextSequenceSearcher() };
            _hashesButton = new MenuBarButton { Text = LocalizationResources.MenuToolsHashes() };
            _rawImageViewerButton = new MenuBarButton { Text = LocalizationResources.MenuToolsRawImageViewer() };

            _encryptButton = new MenuBarButton { Text = LocalizationResources.MenuCiphersEncrypt() };
            _decryptButton = new MenuBarButton { Text = LocalizationResources.MenuCiphersDecrypt() };

            _compressButton = new MenuBarButton { Text = LocalizationResources.MenuCompressionsDecompress() };
            _decompressButton = new MenuBarButton { Text = LocalizationResources.MenuCompressionsCompress() };

            _includeDevBuildsButton = new MenuBarCheckBox
            {
                Text = LocalizationResources.MenuSettingsIncludeDevBuilds(),
                Checked = Settings.Default.IncludeDevBuilds
            };
            _changeLanguageMenu = new MenuBarRadio { Text = LocalizationResources.MenuSettingsChangeLanguage() };
            _changeThemeMenu = new MenuBarRadio { Text = LocalizationResources.MenuSettingsChangeTheme() };

            _aboutButton = new MenuBarButton { Text = LocalizationResources.MenuAboutTitle() };

            AddLanguages(_changeLanguageMenu);
            AddThemes(_changeThemeMenu);

            #region Main menu bar

            var mainMenuBar = new MainMenuBar
            {
                Items =
                {
                    new MenuBarMenu{Text = LocalizationResources.MenuFile(), Items =
                    {
                        _openButton,
                        _openWithButton,
                        new MenuBarSplitter(),
                        _saveAllButton
                    }},
                    new MenuBarMenu{Text = LocalizationResources.MenuTools(), Items =
                    {
                        _batchExtractButton,
                        _batchInjectButton,
                        _textSequencerButton,
                        _hashesButton,
                        _rawImageViewerButton
                    }},
                    new MenuBarMenu{Text = LocalizationResources.MenuCiphers(), Items =
                    {
                        _encryptButton,
                        _decryptButton
                    }},
                    new MenuBarMenu{Text = LocalizationResources.MenuCompressions(), Items =
                    {
                        _compressButton,
                        _decompressButton
                    }},
                    new MenuBarMenu{Text = LocalizationResources.MenuSettings(), Items =
                    {
                        _includeDevBuildsButton,
                        _changeLanguageMenu,
                        _changeThemeMenu
                    }},
                    new MenuBarMenu{Text = LocalizationResources.MenuHelp(), Items =
                    {
                        _aboutButton
                    }}
                }
            };

            #endregion

            #region Main Content

            _tabControl = new TabControl();
            _progressBar = new ProgressBar { Size = new Size(.5f, 24), ProgressColor = ColorResources.Progress };
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

            DefaultFont = FontResources.Arial(15);

            Icon = ImageResources.Icon;
            AllowDragDrop = true;

            Size = new Vector2(1116, 643);
            Style.SetStyle(ImGuiStyleVar.WindowPadding, new Vector2(4));

            MainMenuBar = mainMenuBar;
            Content = mainLayout;

            #endregion
        }

        private void AddLanguages(MenuBarRadio menu)
        {
            menu.CheckItems.Clear();

            foreach (var locale in LocalizationResources.GetLocales())
            {
                var checkBox = new MenuBarCheckBox
                {
                    Text = LocalizationResources.GetLanguageName(locale),
                    Checked = Settings.Default.Locale == locale
                };

                menu.CheckItems.Add(checkBox);
            }
        }

        private void AddThemes(MenuBarRadio menu)
        {
            var lightCheckBox = new MenuBarCheckBox { Text = LocalizationResources.MenuSettingsChangeThemeLight(), Checked = Settings.Default.Theme == Theme.Light.ToString() };
            var darkCheckBox = new MenuBarCheckBox { Text = LocalizationResources.MenuSettingsChangeThemeDark(), Checked = Settings.Default.Theme == Theme.Dark.ToString() };

            _themes.Clear();
            _themes[lightCheckBox] = Theme.Light;
            _themes[darkCheckBox] = Theme.Dark;

            menu.CheckItems.Clear();
            menu.CheckItems.Add(lightCheckBox);
            menu.CheckItems.Add(darkCheckBox);
        }
    }
}
