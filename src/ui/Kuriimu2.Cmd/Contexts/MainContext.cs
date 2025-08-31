using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Konnect.Contract.DataClasses.Management.Files;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Management.Plugin;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.Game;
using Konnect.Contract.Progress;
using Konnect.Management.Plugin;
using Kuriimu2.Cmd.Models.Contexts;
using Kuriimu2.Cmd.Update;

namespace Kuriimu2.Cmd.Contexts
{
    class MainContext(IPluginManager pluginManager, IFileManager fileManager, IProgressContext progressContext)
        : BaseFileContext(pluginManager, fileManager, progressContext)
    {
        protected override Command[] GetCommandsInternal()
        {
            Command[] baseCommands = base.GetCommandsInternal();

            return baseCommands.Concat([
                new Command("update"),
                new Command("extensions"),
                new Command("list-plugins"),
                new Command("exit")
            ]).ToArray();
        }

        protected override async Task<IContext?> ExecuteNextInternal(Command command, IList<string> arguments)
        {
            IContext? executeContext = await base.ExecuteNextInternal(command, arguments);
            if (executeContext is not null)
                return executeContext;

            switch (command.Name)
            {
                case "update":
                    await Update();
                    return null;

                case "extensions":
                    return new ExtensionContext(PluginManager, FileManager, this, Progress);

                case "list-plugins":
                    ListPlugins();
                    return this;

                case "exit":
                    CloseAll();
                    return null;
            }

            return null;
        }

        protected override bool IsLoaded(string filePath, out IFileState? loadedFile)
        {
            loadedFile = null;

            if (!FileManager.IsLoaded(filePath))
                return false;

            loadedFile = FileManager.GetLoadedFile(filePath);
            return true;
        }

        protected override bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        protected override Task<LoadResult> LoadFileInternal(string filePath, Guid pluginId)
        {
            return pluginId == Guid.Empty ?
                FileManager.LoadFile(filePath) :
                FileManager.LoadFile(filePath, pluginId);
        }

        private async Task Update()
        {
            string? executablePath = await UpdateUtilities.DownloadUpdateExecutable();
            if (executablePath is null)
                return;

            var process = new Process
            {
                StartInfo = new ProcessStartInfo(executablePath, $"{Program.ApplicationType}.{Program.GetCurrentPlatform()} {Path.GetFileName(Process.GetCurrentProcess().MainModule!.FileName)}")
            };
            process.Start();
        }

        private void ListPlugins()
        {
            IFilePlugin[] filePlugins = PluginManager.GetPlugins<IFilePlugin>().ToArray();

            foreach (IFilePlugin filePlugin in filePlugins.OrderBy(x => x.PluginType))
            {
                Console.WriteLine($"[{filePlugin.PluginId}] - {filePlugin.Metadata.Name} | {string.Join(';', filePlugin.FileExtensions)}");
            }
        }
    }
}
