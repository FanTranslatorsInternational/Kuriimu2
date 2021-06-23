using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Kontract.Interfaces.Progress;
using Kontract.Models;
using Kore.Managers.Plugins;
using Kore.Update;

namespace Kuriimu2.Cmd.Contexts
{
    class MainContext : BaseFileContext
    {
        public MainContext(IInternalFileManager pluginManager, IProgressContext progressContext) :
            base(pluginManager, progressContext)
        {
        }

        protected override IList<Command> InitializeCommands()
        {
            var baseCommands = base.InitializeCommands();

            return baseCommands.Concat(new[]
            {
                new Command("update"),
                new Command("extensions"),
                new Command("exit")
            }).ToArray();
        }

        protected override async Task<IContext> ExecuteNextInternal(Command command, IList<string> arguments)
        {
            var executeContext = await base.ExecuteNextInternal(command, arguments);
            if (executeContext != null)
                return executeContext;

            switch (command.Name)
            {
                case "update":
                    Update();
                    return null;

                case "extensions":
                    return new ExtensionContext(PluginManager, this, Progress);

                case "exit":
                    CloseAll();
                    return null;
            }

            return null;
        }

        protected override bool IsLoaded(string filePath)
        {
            return PluginManager.IsLoaded(filePath);
        }

        protected override bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        protected override Task<LoadResult> LoadFileInternal(string filePath, Guid pluginId)
        {
            return pluginId == Guid.Empty ?
                PluginManager.LoadFile(filePath) :
                PluginManager.LoadFile(filePath, pluginId);
        }

        private void Update()
        {
            var executablePath = UpdateUtilities.DownloadUpdateExecutable();
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(executablePath, $"{Program.ApplicationType}.{GetCurrentPlatform()} {Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName)}")
            };
            process.Start();
        }

        private string GetCurrentPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "Mac";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "Windows";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "Linux";

            throw new InvalidOperationException($"The platform {RuntimeInformation.OSDescription} is not supported.");
        }
    }
}
