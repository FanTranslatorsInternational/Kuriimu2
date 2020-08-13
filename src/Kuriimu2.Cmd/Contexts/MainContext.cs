using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kontract;
using Kontract.Extensions;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.IO;
using Kore.Managers.Plugins;

namespace Kuriimu2.CommandLine.Contexts
{
    // TODO: Implement manual selection request by plugin manager
    class MainContext : BaseContext
    {
        private readonly IInternalPluginManager _pluginManager;
        private readonly IList<IStateInfo> _loadedFiles;

        protected override IList<Command> Commands { get; } = new List<Command>
        {
            new Command("open", "file"),
            new Command("open-with", "file", "plugin-id"),
            new Command("save", "file-index"),
            new Command("save-as", "file-index", "save-path"),
            new Command("close","file-index"),
            new Command("close-all"),
            new Command("list"),
            new Command("select", "list-id"),
            new Command("exit")
        };

        public MainContext(IInternalPluginManager pluginManager)
        {
            ContractAssertions.IsNotNull(pluginManager, nameof(pluginManager));

            _pluginManager = pluginManager;
            _loadedFiles = new List<IStateInfo>();
        }

        protected override async Task<IContext> ExecuteNextInternal(Command command, IList<string> arguments)
        {
            switch (command.Name)
            {
                case "open":
                    await LoadFile(arguments[0], Guid.Empty);
                    return this;

                case "open-with":
                    var pluginId = Guid.Parse(arguments[1]);
                    await LoadFile(arguments[0], pluginId);
                    return this;

                case "save":
                    await SaveFile(arguments[0], UPath.Empty);
                    return this;

                case "save-as":
                    await SaveFile(arguments[0], arguments[1]);
                    return this;

                case "close":
                    CloseFile(arguments[0]);
                    return this;

                case "close-all":
                    _pluginManager.CloseAll();
                    return this;

                case "list":
                    ListLoadedFiles();
                    return this;

                case "select":
                    return SelectFile(arguments[0]);

                case "exit":
                    _pluginManager.CloseAll();
                    return null;
            }

            return null;
        }

        private async Task LoadFile(string file, Guid pluginId)
        {
            if (_pluginManager.IsLoaded(file))
            {
                Console.WriteLine($"File '{file}' already loaded.");
                return;
            }

            LoadResult loadResult;
            try
            {
                loadResult = pluginId == Guid.Empty ?
                    await _pluginManager.LoadFile(file) :
                    await _pluginManager.LoadFile(file, pluginId);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Load Error: {e.Message}");
                return;
            }

            if (!loadResult.IsSuccessful)
            {
                Console.WriteLine($"Load Error: {loadResult.Message}");
                return;
            }

            if (loadResult.LoadedState.PluginState is IHexState)
            {
                Console.WriteLine("No plugin supports this file.");
                return;
            }

            _loadedFiles.Add(loadResult.LoadedState);

            Console.WriteLine($"Loaded '{file}' successfully.");
        }

        private async Task SaveFile(string fileIndexArgument, UPath savePath)
        {
            if (!int.TryParse(fileIndexArgument, out var fileIndex))
            {
                Console.WriteLine($"'{fileIndexArgument}' is not a valid index.");
                return;
            }

            if (fileIndex >= _loadedFiles.Count)
            {
                Console.WriteLine($"Index '{fileIndexArgument}' was out of bounds.");
                return;
            }

            var loadedFile = _loadedFiles[fileIndex];
            if (!(loadedFile.PluginState is ISaveFiles))
            {
                Console.WriteLine($"File '{loadedFile.FilePath}' is not savable.");
                return;
            }

            var saveResult = savePath == UPath.Empty ?
                await _pluginManager.SaveFile(_loadedFiles[fileIndex]) :
                await _pluginManager.SaveFile(_loadedFiles[fileIndex], savePath);

            if (!saveResult.IsSuccessful)
            {
                Console.WriteLine($"Save Error: {saveResult.Message}");
                return;
            }

            Console.WriteLine($"File '{loadedFile.FilePath}' saved successfully.");
        }

        private void CloseFile(string fileIndexArgument)
        {
            if (!int.TryParse(fileIndexArgument, out var fileIndex))
            {
                Console.WriteLine($"'{fileIndexArgument}' is not a valid index.");
                return;
            }

            if (fileIndex >= _loadedFiles.Count)
            {
                Console.WriteLine($"Index '{fileIndexArgument}' was out of bounds.");
                return;
            }

            var loadedFile = _loadedFiles[fileIndex];
            _pluginManager.Close(loadedFile);
        }

        private void ListLoadedFiles()
        {
            for (var i = 0; i < _loadedFiles.Count; i++)
            {
                var loadedFile = _loadedFiles[i];
                Console.WriteLine($"[{i}] {loadedFile.FilePath.GetName()} - {loadedFile.FilePlugin.Metadata.Name} - {loadedFile.FilePlugin.PluginId}");
            }
        }

        private IContext SelectFile(string argument)
        {
            if (!int.TryParse(argument, out var index))
            {
                Console.WriteLine($"'{argument}' is not a valid index.");
                return this;
            }

            if (index >= _loadedFiles.Count)
            {
                Console.WriteLine($"Index '{index}' was out of bounds.");
                return this;
            }

            switch (_loadedFiles[index].PluginState)
            {
                case ITextState _:
                    return new TextContext(_loadedFiles[index], this);

                case IImageState _:
                    return new ImageContext(_loadedFiles[index], this);

                case IArchiveState _:
                    return new ArchiveContext(_loadedFiles[index], this);

                default:
                    Console.WriteLine($"State '{_loadedFiles[index].PluginState.GetType()}' is not supported.");
                    return this;
            }
        }
    }
}
