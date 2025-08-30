using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Management.Plugin;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Progress;
using Konnect.FileSystem;
using Konnect.Management.Streams;
using Kuriimu2.Cmd.Batch;
using Kuriimu2.Cmd.Models.Contexts;
using Serilog;
using Serilog.Core;

namespace Kuriimu2.Cmd.Contexts
{
    class ExtensionContext : BaseContext
    {
        private readonly IPluginManager _pluginManager;
        private readonly IContext _parentContext;

        private readonly BatchExtractor _batchExtractor;
        private readonly BatchInjector _batchInjector;

        public ExtensionContext(IPluginManager pluginManager, IFileManager fileManager, IContext parentContext, IProgressContext progressContext) :
            base(progressContext)
        {
            Logger logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

            _pluginManager = pluginManager;
            _parentContext = parentContext;
            _batchExtractor = new BatchExtractor(fileManager, logger);
            _batchInjector = new BatchInjector(fileManager, logger);
        }

        protected override Command[] GetCommandsInternal()
        {
            return
            [
                new Command("batch-extract", "input-dir", "output-dir"),
                new Command("batch-extract-with", "input-dir", "output-dir", "plugin-id"),
                new Command("batch-inject", "input-dir", "output-dir"),
                new Command("batch-inject-with", "input-dir", "output-dir", "plugin-id"),
                new Command("back")
            ];
        }

        protected override async Task<IContext?> ExecuteNextInternal(Command command, IList<string> arguments)
        {
            switch (command.Name)
            {
                case "batch-extract":
                    await BatchExtract(arguments[0], arguments[1], null);
                    return this;

                case "batch-extract-with":
                    await BatchExtract(arguments[0], arguments[1], arguments[2]);
                    return this;

                case "batch-inject":
                    await BatchInject(arguments[0], arguments[1], null);
                    return this;

                case "batch-inject-with":
                    await BatchInject(arguments[0], arguments[1], arguments[2]);
                    return this;

                case "back":
                    return _parentContext;
            }

            return null;
        }

        private async Task BatchExtract(UPath inputDirectory, UPath outputDirectory, string? pluginIdArgument)
        {
            if (!TryParseGuidArgument(pluginIdArgument, out Guid pluginId))
                return;

            var plugin = _pluginManager.GetPlugin<IFilePlugin>(pluginId);
            if (plugin is null)
                return;

            IFileSystem sourceFileSystem = FileSystemFactory.CreateSubFileSystem(inputDirectory.FullName, new StreamManager());
            IFileSystem destinationFileSystem = FileSystemFactory.CreateSubFileSystem(outputDirectory.FullName, new StreamManager());

            _batchExtractor.ScanSubDirectories = true;
            _batchExtractor.Plugin = plugin;

            await _batchExtractor.Process(sourceFileSystem, destinationFileSystem);
        }

        private async Task BatchInject(UPath inputDirectory, UPath outputDirectory, string? pluginIdArgument)
        {
            if (!TryParseGuidArgument(pluginIdArgument, out Guid pluginId))
                return;

            var plugin = _pluginManager.GetPlugin<IFilePlugin>(pluginId);
            if (plugin is null)
                return;

            IFileSystem sourceFileSystem = FileSystemFactory.CreateSubFileSystem(inputDirectory.FullName, new StreamManager());
            IFileSystem destinationFileSystem = FileSystemFactory.CreateSubFileSystem(outputDirectory.FullName, new StreamManager());

            _batchInjector.ScanSubDirectories = true;
            _batchInjector.Plugin = plugin;

            await _batchInjector.Process(sourceFileSystem, destinationFileSystem);
        }

        private static bool TryParseGuidArgument(string? pluginIdArgument, out Guid pluginId)
        {
            pluginId = Guid.Empty;

            if (string.IsNullOrEmpty(pluginIdArgument) ||
                Guid.TryParse(pluginIdArgument, out pluginId))
                return true;

            Console.WriteLine($"'{pluginIdArgument}' is not a valid plugin ID.");
            return false;
        }
    }
}
