using Kuriimu2.Cmd.Update;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Kuriimu2.Cmd.Models;
using Kuriimu2.Cmd.Resources;

namespace Kuriimu2.Cmd.Processors
{
    internal class UpdateProcessor
    {
        private const string ManifestUrl = "https://raw.githubusercontent.com/FanTranslatorsInternational/Kuriimu2-ImGuiForms-Update/main/{0}/manifest.json";
        private const string ApplicationType = "CommandLine";

        public static async Task Update()
        {
            var platform = GetCurrentPlatform();

            var remoteManifest = await UpdateUtilities.GetRemoteManifestAsync(string.Format(ManifestUrl, platform));
            var localManifest = JsonSerializer.Deserialize(BinaryResources.VersionManifest!, ManifestJsonSerializerContext.Default.Manifest);

            if (!UpdateUtilities.IsUpdateAvailable(remoteManifest, localManifest, true))
                return;

            string? executablePath = await UpdateUtilities.DownloadUpdateExecutableAsync();
            if (executablePath is null)
                return;

            var process = new Process
            {
                StartInfo = new ProcessStartInfo(executablePath, $"{ApplicationType}.{platform} {Path.GetFileName(Process.GetCurrentProcess().MainModule!.FileName)}")
            };
            process.Start();
        }

        private static string GetCurrentPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "Linux";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "Windows";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "Mac";

            throw new InvalidOperationException($"Unsupported platform {RuntimeInformation.OSDescription}.");
        }
    }
}
