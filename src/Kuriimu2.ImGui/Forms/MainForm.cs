using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ImGui.Forms;
using ImGui.Forms.Controls.Menu;
using ImGui.Forms.Modals;
using ImGui.Forms.Models;
using Kore.Models.Update;
using Kore.Update;
using Kuriimu2.ImGui.Resources;
using Newtonsoft.Json;

namespace Kuriimu2.ImGui.Forms
{
    partial class MainForm : Form
    {
        private readonly Manifest _localManifest;

        #region Constants

        private const string ManifestUrl_ = "https://raw.githubusercontent.com/FanTranslatorsInternational/Kuriimu2-ImGuiForms-Update/main/{0}/manifest.json";
        private const string ApplicationType_ = "ImGui";

        #endregion

        public MainForm()
        {
            InitializeComponent();

            #region Initialization

            _localManifest = LoadLocalManifest();

            #endregion

            #region Events

            Load += MainForm_Load;

            _includeDevBuildsButton.CheckChanged += _includeDevBuildsButton_CheckChanged;
            _changeLanguageMenu.SelectedItemChanged += _changeLanguageMenu_SelectedItemChanged;
            _changeThemeMenu.SelectedItemChanged += _changeThemeMenu_SelectedItemChanged;

            #endregion
        }

        #region Events

        #region Form

        private async void MainForm_Load(object sender, EventArgs e)
        {
#if DEBUG
            await CheckForUpdate();
#endif
        }

        #endregion

        #region Change application settings

        private void _includeDevBuildsButton_CheckChanged(object sender, EventArgs e)
        {
            Settings.Default.IncludeDevBuilds = ((MenuBarCheckBox)sender).Checked;
            Settings.Default.Save();
        }

        private void _changeLanguageMenu_SelectedItemChanged(object sender, EventArgs e)
        {
            var locale = LocalizationResources.GetLocaleByName(((MenuBarRadio)sender).SelectedItem.Caption);

            Settings.Default.Locale = locale;
            Settings.Default.Save();

            LocalizationResources.Instance.ChangeLocale(locale);
        }

        private void _changeThemeMenu_SelectedItemChanged(object sender, EventArgs e)
        {
            var theme = ((MenuBarRadio)sender).SelectedItem.Caption;

            if (!Enum.TryParse<Theme>(theme, out _))
                return;

            Settings.Default.Theme = theme;
            Settings.Default.Save();
        }

        #endregion

        #endregion

        #region Support

        private Manifest LoadLocalManifest()
        {
            return JsonConvert.DeserializeObject<Manifest>(BinaryResources.VersionManifest);
        }

        private async Task CheckForUpdate()
        {
            if (_localManifest == null)
                return;

            var platform = GetCurrentPlatform();

            var remoteManifest = UpdateUtilities.GetRemoteManifest(string.Format(ManifestUrl_, platform));
            if (!UpdateUtilities.IsUpdateAvailable(remoteManifest, _localManifest, Settings.Default.IncludeDevBuilds))
                return;

            var result = await MessageBox.ShowYesNoAsync(
                LocalizationResources.UpdateAvailableResource(_localManifest.Version, _localManifest.BuildNumber, remoteManifest.Version, remoteManifest.BuildNumber),
                LocalizationResources.UpdateAvailableCaptionResource());
            if (result == DialogResult.No)
                return;

            var executablePath = UpdateUtilities.DownloadUpdateExecutable();
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(executablePath, $"{ApplicationType_}{platform} {Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName)}")
            };
            process.Start();

            Close();
        }

        private string GetCurrentPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "Mac";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "Windows";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "Linux";

            throw new InvalidOperationException($"The platform {RuntimeInformation.OSDescription} is not supported.");
        }

        #endregion
    }
}
