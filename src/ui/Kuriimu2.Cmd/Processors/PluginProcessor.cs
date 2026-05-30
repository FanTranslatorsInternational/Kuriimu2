using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Konnect.Contract.Management.Plugin;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.Game;
using Konnect.Management.Plugin;
using Konnect.Management.Plugin.Loaders;

namespace Kuriimu2.Cmd.Processors
{
    internal abstract class PluginProcessor
    {
        protected IPluginManager? CreatePluginManager()
        {
            string? baseDirectory = GetBaseDirectory();
            if (baseDirectory is null)
                return null;

            string pluginPath = Path.Combine(baseDirectory, "plugins");

            var fileLoader = new PluginLoader<IFilePlugin>(pluginPath);
            var gameLoader = new PluginLoader<IGamePlugin>(pluginPath);

            return new PluginManager(fileLoader, gameLoader);
        }

        private static string? GetBaseDirectory()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return ".";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                string? path = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location);
                if (string.IsNullOrEmpty(path))
                    path = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName);

                return path;
            }

            Console.WriteLine($"Unsupported operating system {RuntimeInformation.OSDescription}.");
            return null;
        }
    }
}
