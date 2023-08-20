using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Kontract.Interfaces.Managers;
using Kontract.Models;
using Kore.Managers.Plugins;
using Kore.Models.Update;
using Kore.Progress;
using Kore.Update;
using Kuriimu2.Cmd.Contexts;
using Kuriimu2.Cmd.Manager;
using Kuriimu2.Cmd.Parsers;
using Kuriimu2.Cmd.Progress;
using Newtonsoft.Json;
using Serilog;

namespace Kuriimu2.Cmd
{
    class Program
    {
        private const string ManifestUrl = "https://raw.githubusercontent.com/FanTranslatorsInternational/Kuriimu2-CommandLine-Update/main/{0}/manifest.json";
        public const string ApplicationType = "CommandLine.{0}";

        private static Manifest _localManifest;
        private static IArgumentGetter _argumentGetter;

        static void Main(string[] args)
        {
            _argumentGetter = new ArgumentGetter(args);
            _localManifest = LoadLocalManifest();

            PrintWelcomeText();
            CheckForUpdate();

            var progressContext = new ProgressContext(new ConsoleProgressOutput(14));
            var dialogManager = new ConsoleDialogManager(_argumentGetter, progressContext);
            var logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
            var pluginManager = new FileManager("plugins")
            {
                DialogManager = dialogManager,
                Progress = progressContext,
                Logger = logger
            };
            pluginManager.OnManualSelection += PluginManager_OnManualSelection;

            PrintUnloadedPlugins(pluginManager.LoadErrors);

            IContext context = new MainContext(pluginManager, progressContext);

            while (context != null)
            {
                context.PrintCommands();

                context = context.ExecuteNext(_argumentGetter).Result;
            }
        }

        private static void PrintWelcomeText()
        {
            Console.WriteLine($"Welcome to Kuriimu2 {_localManifest.Version}-{_localManifest.BuildNumber}");
            Console.WriteLine();
            Console.WriteLine("Authors: onepiecefreak, IcySon55, Neobeo, and other contributors");
            Console.WriteLine("Github: https://github.com/FanTranslatorsInternational/Kuriimu2");
        }

        private static void CheckForUpdate()
        {
            var remoteManifest = UpdateUtilities.GetRemoteManifest(string.Format(ManifestUrl, GetCurrentPlatform()));
            if (!UpdateUtilities.IsUpdateAvailable(remoteManifest, _localManifest, true))
                return;

            Console.WriteLine();
            Console.WriteLine($"A new version is available: {remoteManifest.Version}-{remoteManifest.BuildNumber}");
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

        private static Manifest LoadLocalManifest()
        {
            var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Kuriimu2.Cmd.Resources.version.json");
            if (resourceStream == null)
                return null;

            return JsonConvert.DeserializeObject<Manifest>(new StreamReader(resourceStream).ReadToEnd());
        }

        private static void PrintUnloadedPlugins(IReadOnlyList<PluginLoadError> loadErrors)
        {
            if (!loadErrors.Any())
                return;

            Console.WriteLine();
            Console.WriteLine("Some plugins could not be loaded:");
            foreach (var loadError in loadErrors)
                Console.WriteLine($"\t{loadError.AssemblyPath} - {loadError.Exception.Message}");
        }

        private static void PluginManager_OnManualSelection(object sender, ManualSelectionEventArgs e)
        {
            Console.WriteLine("No plugin could identify the file!");
            Console.WriteLine("Select a plugin manually:");

            foreach (var filePlugin in e.FilePlugins)
                Console.WriteLine($"[{filePlugin.PluginId}] - {filePlugin.Metadata.Name} | {string.Join(';', filePlugin.FileExtensions)}");

            var idArgument = _argumentGetter.GetNextArgument();

            if (!Guid.TryParse(idArgument, out var pluginId))
            {
                Console.WriteLine($"'{idArgument}' is not a valid plugin ID.");
                e.Result = null;
                return;
            }

            Console.Clear();
            e.Result = e.FilePlugins.FirstOrDefault(x => x.PluginId == pluginId);
        }
    }
}
