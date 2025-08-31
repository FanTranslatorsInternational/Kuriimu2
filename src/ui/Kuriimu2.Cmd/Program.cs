using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Konnect.Contract.DataClasses.Management.Files.Events;
using Konnect.Contract.DataClasses.Management.Plugin.Loaders;
using Konnect.Contract.Management.Plugin;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.Game;
using Konnect.Management.Files;
using Konnect.Management.Plugin;
using Konnect.Management.Plugin.Loaders;
using Konnect.Progress;
using Kuriimu2.Cmd.Contexts;
using Kuriimu2.Cmd.Manager;
using Kuriimu2.Cmd.Models;
using Kuriimu2.Cmd.Parsers;
using Kuriimu2.Cmd.Progress;
using Kuriimu2.Cmd.Resources;
using Kuriimu2.Cmd.Update;
using Serilog;
using Serilog.Core;

namespace Kuriimu2.Cmd
{
    class Program
    {
        private const string ManifestUrl_ = "https://raw.githubusercontent.com/FanTranslatorsInternational/Kuriimu2-CommandLine-Update/main/{0}/manifest.json";
        public const string ApplicationType = "CommandLine";

        private static IArgumentGetter? _argumentGetter;

        static async Task Main(string[] args)
        {
            Manifest? localManifest = LoadLocalManifest();

            PrintWelcomeText(localManifest);
            await CheckForUpdate(localManifest);

            _argumentGetter = new ArgumentGetter(args);

            var progressContext = new ProgressContext(new ConsoleProgressOutput(14));
            var dialogManager = new ConsoleDialogManager(_argumentGetter, progressContext);
            Logger logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

            IPluginManager? pluginManager = LoadPluginManager();
            if (pluginManager is null)
            {
                Console.WriteLine("No plugins were loaded.");
                return;
            }

            var fileManager = new FileManager(pluginManager)
            {
                DialogManager = dialogManager,
                Progress = progressContext,
                Logger = logger
            };
            fileManager.OnManualSelection += PluginManager_OnManualSelection;

            PrintUnloadedPlugins(pluginManager.GetErrors());

            IContext? context = new MainContext(pluginManager, fileManager, progressContext);

            while (context is not null)
            {
                context.PrintCommands();

                context = await context.ExecuteNext(_argumentGetter);
            }
        }

        #region Update

        private static Manifest? LoadLocalManifest()
        {
            string? manifest = BinaryResources.VersionManifest;
            return manifest is null ? null : JsonSerializer.Deserialize<Manifest>(manifest);
        }

        private static async Task CheckForUpdate(Manifest? localManifest)
        {
            string platform = GetCurrentPlatform();

            Manifest? remoteManifest = await UpdateUtilities.GetRemoteManifest(string.Format(ManifestUrl_, platform));
            if (!UpdateUtilities.IsUpdateAvailable(remoteManifest, localManifest, true))
                return;

            Console.WriteLine();
            Console.WriteLine($"A new version is available: {remoteManifest!.Version}-{remoteManifest.BuildNumber}");
        }

        public static string GetCurrentPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "Linux";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "Windows";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "Mac";

            throw new InvalidOperationException($"Unsupported platform {RuntimeInformation.OSDescription}.");
        }

        #endregion

        #region Plugins

        private static IPluginManager? LoadPluginManager()
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
                string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                if (string.IsNullOrEmpty(path))
                    path = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

                return path;
            }

            Console.WriteLine($"Unsupported operating system {RuntimeInformation.OSDescription}.");
            return null;
        }

        #endregion

        #region Print

        private static void PrintWelcomeText(Manifest? localManifest)
        {
            Console.WriteLine(localManifest is null
                ? "Welcome to Kuriimu2"
                : $"Welcome to Kuriimu2 {localManifest.Version}-{localManifest.BuildNumber}");
            Console.WriteLine();
            Console.WriteLine("Authors: onepiecefreak, IcySon55, Neobeo, and other contributors");
            Console.WriteLine("Github: https://github.com/FanTranslatorsInternational/Kuriimu2");
        }

        private static void PrintUnloadedPlugins(IReadOnlyList<PluginLoadError> loadErrors)
        {
            if (!loadErrors.Any())
                return;

            Console.WriteLine();
            Console.WriteLine("Some plugins could not be loaded:");

            foreach (PluginLoadError loadError in loadErrors)
                Console.WriteLine($"\t{loadError.AssemblyPath} - {loadError.Exception.Message}");
        }

        private static Task PluginManager_OnManualSelection(ManualSelectionEventArgs e)
        {
            if (_argumentGetter is null)
                return Task.CompletedTask;

            Console.WriteLine("No plugin could identify the file.");

            if (e.FilePlugins.Length <= 0)
                return Task.CompletedTask;

            Console.WriteLine();
            Console.WriteLine("Select a plugin manually:");

            foreach (IFilePlugin filePlugin in e.FilePlugins)
                Console.WriteLine($"[{filePlugin.PluginId}] - {filePlugin.Metadata.Name} | {string.Join(';', filePlugin.FileExtensions)}");

            string idArgument = _argumentGetter.GetNextArgument();

            if (!Guid.TryParse(idArgument, out Guid pluginId))
            {
                Console.WriteLine($"'{idArgument}' is not a valid plugin ID.");
                e.Result = null;

                return Task.CompletedTask;
            }

            Console.Clear();
            e.Result = e.FilePlugins.FirstOrDefault(x => x.PluginId == pluginId);

            return Task.CompletedTask;
        }

        #endregion
    }
}
