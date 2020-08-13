using System;
using System.Collections.Generic;
using System.Linq;
using Kontract.Models;
using Kore.Managers.Plugins;
using Kore.Progress;
using Kuriimu2.Cmd.Progress;
using Kuriimu2.CommandLine.Contexts;
using Kuriimu2.CommandLine.Parsers;

namespace Kuriimu2.CommandLine
{
    class Program
    {
        // TODO: Add dialog manager
        static void Main(string[] args)
        {
            PrintWelcomeText();

            var progressContext = new ProgressContext(new ConsoleProgressOutput(14));
            var pluginManager = new PluginManager(progressContext, "plugins");

            PrintUnloadedPlugins(pluginManager.LoadErrors);

            IContext context = new MainContext(pluginManager);
            var argumentGetter = new ArgumentGetter(args);

            while (context != null)
            {
                context.PrintCommands();

                context = context.ExecuteNext(argumentGetter).Result;
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
    }
}
