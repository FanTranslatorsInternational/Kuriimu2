using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kontract;
using Kontract.Interfaces.Progress;
using Kontract.Models.IO;
using Kore.Batch;
using Kore.Factories;
using Kore.Managers;
using Kore.Managers.Plugins;
using Serilog;

namespace Kuriimu2.Cmd.Contexts
{
    class ExtensionContext : BaseContext
    {
        private readonly IContext _parentContext;

        private readonly BatchExtractor _batchExtractor;
        private readonly BatchInjector _batchInjector;

        public ExtensionContext(IInternalFileManager pluginManager, IContext parentContext, IProgressContext progressContext) :
            base(progressContext)
        {
            ContractAssertions.IsNotNull(pluginManager, nameof(pluginManager));
            ContractAssertions.IsNotNull(parentContext, nameof(parentContext));

            var logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

            _parentContext = parentContext;
            _batchExtractor = new BatchExtractor(pluginManager, logger);
            _batchInjector = new BatchInjector(pluginManager, logger);
        }

        protected override IList<Command> InitializeCommands()
        {
            return new[]
            {
                new Command("batch-extract", "input-dir", "output-dir"),
                new Command("batch-extract-with", "input-dir", "output-dir", "plugin-id"),
                new Command("batch-inject", "input-dir", "output-dir"),
                new Command("batch-inject-with", "input-dir", "output-dir", "plugin-id"),
                new Command("back")
            };
        }

        protected override async Task<IContext> ExecuteNextInternal(Command command, IList<string> arguments)
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

        private async Task BatchExtract(UPath inputDirectory, UPath outputDirectory, string pluginIdArgument)
        {
            if (!TryParseGuidArgument(pluginIdArgument, out var pluginId))
                return;

            var sourceFileSystem = FileSystemFactory.CreateSubFileSystem(inputDirectory.FullName, new StreamManager());
            var destinationFileSystem = FileSystemFactory.CreateSubFileSystem(outputDirectory.FullName, new StreamManager());

            _batchExtractor.ScanSubDirectories = true;
            _batchExtractor.PluginId = pluginId;
            await _batchExtractor.Process(sourceFileSystem, destinationFileSystem);
        }

        private async Task BatchInject(UPath inputDirectory, UPath outputDirectory, string pluginIdArgument)
        {
            if (!TryParseGuidArgument(pluginIdArgument, out var pluginId))
                return;

            var sourceFileSystem = FileSystemFactory.CreateSubFileSystem(inputDirectory.FullName, new StreamManager());
            var destinationFileSystem = FileSystemFactory.CreateSubFileSystem(outputDirectory.FullName, new StreamManager());

            _batchInjector.ScanSubDirectories = true;
            _batchInjector.PluginId = pluginId;
            await _batchInjector.Process(sourceFileSystem, destinationFileSystem);
        }

        private bool TryParseGuidArgument(string pluginIdArgument, out Guid pluginId)
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
