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

        protected override IList<Command> Commands { get; } = new List<Command>
        {
            new Command("batch-extract","dir-path"),
            new Command("batch-extract-with","dir-path","plugin-id"),
            new Command("back")
        };

        public ExtensionContext(IInternalPluginManager pluginManager, IContext parentContext, IProgressContext progressContext) :
            base(progressContext)
        {
            ContractAssertions.IsNotNull(pluginManager, nameof(pluginManager));
            ContractAssertions.IsNotNull(parentContext, nameof(parentContext));

            var logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

            _parentContext = parentContext;
            _batchExtractor = new BatchExtractor(pluginManager, logger);
        }

        protected override async Task<IContext> ExecuteNextInternal(Command command, IList<string> arguments)
        {
            switch (command.Name)
            {
                case "batch-extract":
                    await BatchExtract(arguments[0], null);
                    return this;

                case "batch-extract-with":
                    await BatchExtract(arguments[0], arguments[1]);
                    return this;

                case "back":
                    return _parentContext;
            }

            return null;
        }

        private async Task BatchExtract(UPath directory, string pluginIdArgument)
        {
            var pluginId = Guid.Empty;
            if (!string.IsNullOrEmpty(pluginIdArgument))
            {
                if (!Guid.TryParse(pluginIdArgument, out pluginId))
                {
                    Console.WriteLine($"'{pluginIdArgument}' is not a valid plugin ID.");
                    return;
                }
            }

            var sourceFileSystem = FileSystemFactory.CreatePhysicalFileSystem(directory, new StreamManager());
            var destinationFileSystem = FileSystemFactory.CreatePhysicalFileSystem(directory, new StreamManager());

            _batchExtractor.ScanSubDirectories = true;
            _batchExtractor.PluginId = pluginId;
            await _batchExtractor.Process(sourceFileSystem, destinationFileSystem);
        }
    }
}
