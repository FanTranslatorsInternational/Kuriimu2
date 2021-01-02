using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using Kore.Models.Update;
using Newtonsoft.Json;

namespace Kore.Update
{
    public static class UpdateUtilities
    {
        private const string UpdateUrl_ = "https://raw.githubusercontent.com/FanTranslatorsInternational/Kuriimu2-Updater/master/bin";
        private const string ExecutableName_ = "update.exe";

        public static Manifest GetRemoteManifest(string manifestUrl)
        {
            var resourceStream = GetResourceStream(manifestUrl);
            return resourceStream != null ? JsonConvert.DeserializeObject<Manifest>(new StreamReader(resourceStream).ReadToEnd()) : null;
        }

        public static bool IsUpdateAvailable(Manifest remoteManifest, Manifest localManifest)
        {
            if (remoteManifest == null || localManifest == null)
                return false;

            return remoteManifest.SourceType != localManifest.SourceType || remoteManifest.BuildNumber != localManifest.BuildNumber;
        }

        public static string DownloadUpdateExecutable()
        {
            var platform = GetCurrentPlatform();

            var updateUrl = UpdateUrl_ + "/" + platform + "/" + ExecutableName_;
            var resourceStream = GetResourceStream(updateUrl);
            var currentDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

            var executablePath = currentDirectory + "/" + ExecutableName_;
            var executableFileStream = File.Open(executablePath, FileMode.Create);

            resourceStream.CopyTo(executableFileStream);

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

        private static Stream GetResourceStream(string resourceUrl)
        {
            var request = WebRequest.CreateHttp(resourceUrl);

            Stream responseStream;
            try
            {
                responseStream = request.GetResponse().GetResponseStream();
            }
            catch
            {
                return null;
            }

            return responseStream != null ? ToMemoryStream(responseStream) : null;
        }

        private static Stream ToMemoryStream(Stream input)
        {
            var ms = new MemoryStream();

            var buffer = new byte[4096];
            while (true)
            {
                var readBytes = input.Read(buffer, 0, buffer.Length);
                if (readBytes == 0)
                    break;

                var length = Math.Min(readBytes, buffer.Length);
                ms.Write(buffer, 0, length);
            }

            ms.Position = 0;
            return ms;
        }
    }
}
