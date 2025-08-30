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
        private const string UpdateUrl_ = "https://raw.githubusercontent.com/FanTranslatorsInternational/Kuriimu2-Updater/master/bin";
        private const string ExecutableName_ = "update.exe";

        public static async Task<Manifest?> GetRemoteManifest(string manifestUrl)
        {
            string? resource = await GetResourceString(manifestUrl);
            return resource is null ? null : JsonSerializer.Deserialize<Manifest>(resource);
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

        public static async Task<string?> DownloadUpdateExecutable()
        {
            string platform = GetCurrentPlatform();

            var updateUrl = $"{UpdateUrl_}/{platform}/{ExecutableName_}";
            Stream? resourceStream = await GetResourceStream(updateUrl);
            if (resourceStream is null)
                return null;

            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string executablePath = Path.Combine(currentDirectory, ExecutableName_);

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

        private static async Task<string?> GetResourceString(string resourceUrl)
        {
            var client = new HttpClient();

            try
            {
                return await client.GetStringAsync(resourceUrl);
            }
            catch
            {
                return null;
            }
        }

        private static async Task<Stream?> GetResourceStream(string resourceUrl)
        {
            var client = new HttpClient();

            try
            {
                return await client.GetStreamAsync(resourceUrl);
            }
            catch
            {
                return null;
            }
        }
    }
}
