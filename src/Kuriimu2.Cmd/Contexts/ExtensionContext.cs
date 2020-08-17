using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kanvas;
using Kontract;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.IO;
using Kore.Factories;
using Kore.Managers;
using Kore.Managers.Plugins;

namespace Kuriimu2.Cmd.Contexts
{
    class ExtensionContext : BaseContext
    {
        private readonly IInternalPluginManager _pluginManager;
        private readonly IContext _parentContext;

        protected override IList<Command> Commands { get; } = new List<Command>
        {
            new Command("batch-extract","dir-path"),
            new Command("batch-extract-with","dir-path","plugin-id"),
            new Command("back")
        };

        public ExtensionContext(IInternalPluginManager pluginManager, IContext parentContext)
        {
            ContractAssertions.IsNotNull(pluginManager, nameof(pluginManager));
            ContractAssertions.IsNotNull(parentContext, nameof(parentContext));

            _pluginManager = pluginManager;
            _parentContext = parentContext;
        }

        protected override Task<IContext> ExecuteNextInternal(Command command, IList<string> arguments)
        {
            switch (command.Name)
            {
                case "batch-extract":
                    BatchExtract(arguments[0], null);
                    return Task.FromResult((IContext)this);

                case "batch-extract-with":
                    BatchExtract(arguments[0], arguments[1]);
                    return Task.FromResult((IContext)this);

                case "back":
                    return Task.FromResult(_parentContext);
            }

            return null;
        }

        private void BatchExtract(UPath directory, string pluginIdArgument)
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
            EnumerateFiles(sourceFileSystem).ToArray().AsParallel().ForAll(x => ExtractWith(sourceFileSystem, x, pluginId));
        }

        private IEnumerable<UPath> EnumerateFiles(IFileSystem sourceFileSystem)
        {
            foreach (var dirs in sourceFileSystem.EnumeratePaths(UPath.Root, "*", SearchOption.AllDirectories, SearchTarget.Directory))
            {
                foreach (var file in sourceFileSystem.EnumeratePaths(dirs, "*", SearchOption.TopDirectoryOnly, SearchTarget.File))
                {
                    yield return file;
                }
            }

            foreach (var file in sourceFileSystem.EnumeratePaths(UPath.Root, "*", SearchOption.TopDirectoryOnly, SearchTarget.File))
            {
                yield return file;
            }
        }

        private void ExtractWith(IFileSystem sourceFileSystem, UPath filePath, Guid pluginId)
        {
            // Load file
            LoadResult loadResult;
            try
            {
                loadResult = pluginId == Guid.Empty ?
                    _pluginManager.LoadFile(sourceFileSystem, filePath).Result :
                    _pluginManager.LoadFile(sourceFileSystem, filePath, pluginId).Result;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Batch Error: {filePath}: {e.Message}");
                return;
            }

            if (!loadResult.IsSuccessful)
            {
                Console.WriteLine($"Batch Error: {filePath}: {loadResult.Message}");
                return;
            }

            var absolutePath = (UPath)sourceFileSystem.ConvertPathToInternal(filePath);
            var destinationDirectory = absolutePath.GetDirectory() / absolutePath.GetName().Replace('.', '_');
            var destinationFileSystem = FileSystemFactory.CreatePhysicalFileSystem(destinationDirectory, new StreamManager());

            switch (loadResult.LoadedState.PluginState)
            {
                case IArchiveState archiveState:
                    foreach (var afi in archiveState.Files)
                    {
                        var newFileStream = destinationFileSystem.OpenFile(afi.FilePath, FileMode.Create, FileAccess.Write);
                        afi.GetFileData().Result.CopyTo(newFileStream);

                        newFileStream.Close();
                    }
                    break;

                case IImageState imageState:
                    var index = 0;
                    foreach (var img in imageState.Images)
                    {
                        var kanvasImage = new KanvasImage(imageState, img);
                        kanvasImage.GetImage().Save(destinationDirectory + "/" + (img.Name ?? $"{index:00}") + ".png");

                        index++;
                    }
                    break;

                default:
                    Console.WriteLine($"Batch Error: {filePath}: '{loadResult.LoadedState.PluginState.GetType().Name}' is not supported.");
                    break;
            }

            _pluginManager.Close(loadResult.LoadedState);
        }
    }
}
