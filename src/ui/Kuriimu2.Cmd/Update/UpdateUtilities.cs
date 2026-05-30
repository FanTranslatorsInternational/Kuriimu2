using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Kuriimu2.Cmd.Models;

namespace Kuriimu2.Cmd.Update
{
    internal static class UpdateUtilities
    {
        private const string UpdateUrl = "https://raw.githubusercontent.com/FanTranslatorsInternational/Kuriimu2-Updater/master/bin";
        private const string ExecutableName = "update.exe";

        private static readonly HttpClient Client = new();

        public static async Task<Manifest?> GetRemoteManifestAsync(string manifestUrl)
        {
            var resourceStream = await GetResourceStreamAsync(manifestUrl);
            if (resourceStream is null)
                return null;

            var reader = new StreamReader(resourceStream);
            var text = await reader.ReadToEndAsync();

            return JsonSerializer.Deserialize<Manifest>(text);
        }

        public static bool IsUpdateAvailable(Manifest? remoteManifest, Manifest? localManifest, bool includeDevBuilds)
        {
            if (remoteManifest is null || localManifest is null)
                return false;

            var localVersion = new Version(localManifest.Version);
            var remoteVersion = new Version(remoteManifest.Version);

            bool sourceCheck = remoteManifest.SourceType != localManifest.SourceType;
            bool versionCheck = localVersion < remoteVersion;
            bool buildCheck = remoteManifest.BuildNumber != localManifest.BuildNumber;

            bool result = sourceCheck || versionCheck;
            return includeDevBuilds && localVersion == remoteVersion ? buildCheck : result;
        }

        public static async Task<string?> DownloadUpdateExecutableAsync()
        {
            string platform = GetCurrentPlatform();

            var updateUrl = $"{UpdateUrl}/{platform}/{ExecutableName}";
            Stream? resourceStream = await GetResourceStreamAsync(updateUrl);
            if (resourceStream is null)
                return null;

            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string executablePath = Path.Combine(currentDirectory, ExecutableName);

            await using Stream executableFileStream = File.Open(executablePath, FileMode.Create);

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
