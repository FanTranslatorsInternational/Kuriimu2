using System;
using System.Collections.Generic;
using System.Linq;
using Kontract.Interfaces.Managers;
using Kontract.Models;
using Kore.Managers.Plugins;
using Kore.Progress;
using Kuriimu2.Cmd.Contexts;
using Kuriimu2.Cmd.Parsers;
using Kuriimu2.Cmd.Progress;

namespace Kuriimu2.Cmd
{
    class Program
    {
        private static IArgumentGetter _argumentGetter;

        // TODO: Add dialog manager
        static void Main(string[] args)
        {
            PrintWelcomeText();

            var progressContext = new ProgressContext(new ConsoleProgressOutput(14));
            var pluginManager = new PluginManager(progressContext, "plugins");
            pluginManager.OnManualSelection += PluginManager_OnManualSelection;

            PrintUnloadedPlugins(pluginManager.LoadErrors);

            IContext context = new MainContext(pluginManager);
            _argumentGetter = new ArgumentGetter(args);

            while (context != null)
            {
                context.PrintCommands();

                context = context.ExecuteNext(_argumentGetter).Result;
            }
        }

        private static void PrintWelcomeText()
        {
            Console.WriteLine("Welcome to Kuriimu2");
            Console.WriteLine("\tAuthors: onepiecefreak, IcySon55, Neobeo, and other contributors");
            Console.WriteLine("\tGithub link: https://github.com/FanTranslatorsInternational/Kuriimu2");
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

            e.Result = e.FilePlugins.FirstOrDefault(x => x.PluginId == pluginId);
        }
    }
}
