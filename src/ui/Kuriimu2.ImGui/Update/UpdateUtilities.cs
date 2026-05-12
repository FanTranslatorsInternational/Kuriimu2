using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Kuriimu2.ImGui.Models;
using Version = Kuriimu2.ImGui.Models.Version;

namespace Kuriimu2.ImGui.Update
{
    internal static class UpdateUtilities
    {
        private static readonly HttpClient Client = new();

        private const string UpdateUrl_ = "https://raw.githubusercontent.com/FanTranslatorsInternational/Kuriimu2-Updater/master/bin";
        private const string ExecutableName_ = "update.exe";

        public static async Task<Manifest?> GetRemoteManifestAsync(string manifestUrl)
        {
            var resourceStream = await GetResourceStreamAsync(manifestUrl);
            if (resourceStream is null)
                return null;

            var reader = new StreamReader(resourceStream);
            var text = await reader.ReadToEndAsync();

            return JsonSerializer.Deserialize(text, ManifestJsonSerializerContext.Default.Manifest);
        }

        public static bool IsUpdateAvailable(Manifest? remoteManifest, Manifest? localManifest, bool includeDevBuilds)
        {
            if (remoteManifest is null || localManifest is null)
                return false;

            var localVersion = new Version(localManifest.Version);
            var remoteVersion = new Version(remoteManifest.Version);

            var sourceCheck = remoteManifest.SourceType != localManifest.SourceType;
            var versionCheck = localVersion < remoteVersion;
            var buildCheck = remoteManifest.BuildNumber != localManifest.BuildNumber;

            var result = sourceCheck || versionCheck;
            return includeDevBuilds && localVersion == remoteVersion ? buildCheck : result;
        }

        public static async Task<string?> DownloadUpdateExecutableAsync()
        {
            var platform = GetCurrentPlatform();

            var updateUrl = UpdateUrl_ + "/" + platform + "/" + ExecutableName_;
            var resourceStream = await GetResourceStreamAsync(updateUrl);
            if (resourceStream is null)
                return null;

            var currentDirectory = Path.GetDirectoryName(Environment.ProcessPath) ?? string.Empty;

            var executablePath = Path.Combine(currentDirectory, ExecutableName_);
            var executableFileStream = File.Open(executablePath, FileMode.Create);

            await resourceStream.CopyToAsync(executableFileStream);

            resourceStream.Close();
            executableFileStream.Close();

            return executablePath;
        }

        private static string GetCurrentPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "osx-x64";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "win-x64";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "linux-x64";

            throw new InvalidOperationException($"The platform {RuntimeInformation.OSDescription} is not supported.");
        }

        private static async Task<Stream?> GetResourceStreamAsync(string resourceUrl)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, resourceUrl);

            var response = await Client.SendAsync(request);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsStreamAsync();

            return null;
        }
    }
}
