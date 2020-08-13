using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kontract;
using Kontract.Extensions;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.Archive;
using Kontract.Models.IO;
using Kore.Managers.Plugins;

namespace Kuriimu2.Cmd.Contexts
{
    class MainContext : BaseContext, IMainContext
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
            new Command("select", "file-index"),
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
                    await LoadFile(arguments[0], null);
                    return this;

                case "open-with":
                    await LoadFile(arguments[0], arguments[1]);
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

        public async Task<IStateInfo> LoadFile(IStateInfo stateInfo, ArchiveFileInfo afi, Guid pluginId)
        {
            var absolutePath = stateInfo.AbsoluteDirectory / stateInfo.FilePath / afi.FilePath;
            if (_pluginManager.IsLoaded(absolutePath))
            {
                Console.WriteLine($"File '{afi.FilePath}' already loaded.");
                return null;
            }

            LoadResult loadResult;
            try
            {
                loadResult = pluginId == Guid.Empty ?
                    await _pluginManager.LoadFile(stateInfo, afi) :
                    await _pluginManager.LoadFile(stateInfo, afi, pluginId);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Load Error: {e.Message}");
                return null;
            }

            if (!loadResult.IsSuccessful)
            {
                Console.WriteLine($"Load Error: {loadResult.Message}");
                return null;
            }

            if (loadResult.LoadedState.PluginState is IHexState)
            {
                Console.WriteLine("No plugin supports this file.");
                return null;
            }

            _loadedFiles.Add(loadResult.LoadedState);

            Console.WriteLine($"Loaded '{afi.FilePath}' successfully.");

            return loadResult.LoadedState;
        }

        private async Task LoadFile(string file, string pluginIdArgument)
        {
            if (_pluginManager.IsLoaded(file))
            {
                Console.WriteLine($"File '{file}' already loaded.");
                return;
            }

            var pluginId = Guid.Empty;
            if (string.IsNullOrEmpty(pluginIdArgument))
            {
                if (!Guid.TryParse(pluginIdArgument, out pluginId))
                {
                    Console.WriteLine($"'{pluginIdArgument}' is no valid plugin ID.");
                    return;
                }
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

        private Task SaveFile(string fileIndexArgument, UPath savePath)
        {
            if (!int.TryParse(fileIndexArgument, out var fileIndex))
            {
                Console.WriteLine($"'{fileIndexArgument}' is not a valid index.");
                return Task.CompletedTask;
            }

            if (fileIndex >= _loadedFiles.Count)
            {
                Console.WriteLine($"Index '{fileIndexArgument}' was out of bounds.");
                return Task.CompletedTask;
            }

            var loadedFile = _loadedFiles[fileIndex];
            return SaveFile(loadedFile, savePath);
        }

        public Task SaveFile(IStateInfo stateInfo)
        {
            return SaveFile(stateInfo, UPath.Empty);
        }

        private async Task SaveFile(IStateInfo stateInfo, UPath savePath)
        {
            if (!(stateInfo.PluginState is ISaveFiles))
            {
                Console.WriteLine($"File '{stateInfo.FilePath}' is not savable.");
                return;
            }

            var saveResult = savePath == UPath.Empty ?
                await _pluginManager.SaveFile(stateInfo) :
                await _pluginManager.SaveFile(stateInfo, savePath);

            if (!saveResult.IsSuccessful)
            {
                Console.WriteLine($"Save Error: {saveResult.Message}");
                return;
            }

            Console.WriteLine($"File '{stateInfo.FilePath}' saved successfully.");
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
            CloseFile(loadedFile);
        }

        public void CloseFile(IStateInfo stateInfo)
        {
            _pluginManager.Close(stateInfo);

            if (_loadedFiles.Contains(stateInfo))
                _loadedFiles.Remove(stateInfo);
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

            var fileContext = ContextFactory.CreateFileContext(_loadedFiles[index], this, this);
            return fileContext ?? this;
        }
    }
}
