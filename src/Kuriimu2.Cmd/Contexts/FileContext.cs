using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kontract.Extensions;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kore.Managers.Plugins;

namespace Kuriimu2.Cmd.Contexts
{
    abstract class BaseFileContext : BaseContext
    {
        private readonly IList<IStateInfo> _loadedFiles;

        protected IInternalPluginManager PluginManager { get; }

        public BaseFileContext(IInternalPluginManager pluginManager)
        {
            PluginManager = pluginManager;
            _loadedFiles = new List<IStateInfo>();
        }

        protected override async Task<IContext> ExecuteNextInternal(Command command, IList<string> arguments)
        {
            switch (command.Name)
            {
                case "open":
                    return await LoadFile(arguments[0], null);

                case "open-with":
                    return await LoadFile(arguments[0], arguments[1]);

                case "save":
                    await SaveFile(arguments[0], null);
                    return this;

                case "save-as":
                    await SaveFile(arguments[0], arguments[1]);
                    return this;

                case "close":
                    CloseFile(arguments[0]);
                    return this;

                case "close-all":
                    CloseAll();
                    return this;

                case "select":
                    return SelectFile(arguments[0]);

                case "list-open":
                    ListOpenFiles();
                    return this;
            }

            return null;
        }

        protected abstract bool FileExists(string filePath);

        protected abstract bool IsLoaded(string filePath);

        protected abstract Task<LoadResult> LoadFileInternal(string filePath, Guid pluginId);

        private async Task<IContext> LoadFile(string fileArgument, string pluginIdArgument)
        {
            if (!FileExists(fileArgument))
            {
                Console.WriteLine($"File '{fileArgument}' does not exist.");
                return this;
            }

            if (IsLoaded(fileArgument))
            {
                Console.WriteLine($"File '{fileArgument}' already loaded.");
                return this;
            }

            var pluginId = Guid.Empty;
            if (!string.IsNullOrEmpty(pluginIdArgument))
            {
                if (!Guid.TryParse(pluginIdArgument, out pluginId))
                {
                    Console.WriteLine($"'{pluginIdArgument}' is not a valid plugin ID.");
                    return this;
                }
            }

            LoadResult loadResult;
            try
            {
                loadResult = await LoadFileInternal(fileArgument, pluginId);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Load Error: {e.Message}");
                return this;
            }

            if (!loadResult.IsSuccessful)
            {
                Console.WriteLine($"Load Error: {loadResult.Message}");
                return this;
            }

            if (loadResult.LoadedState.PluginState is IHexState)
            {
                Console.WriteLine("No plugin supports this file.");
                return this;
            }

            _loadedFiles.Add(loadResult.LoadedState);

            Console.WriteLine($"Loaded '{fileArgument}' successfully.");

            return CreateFileContext(loadResult.LoadedState);
        }

        private async Task SaveFile(string fileIndexArgument, string savePathArgument)
        {
            if (!int.TryParse(fileIndexArgument, out var fileIndex))
            {
                Console.WriteLine($"'{fileIndexArgument}' is not a valid number.");
                return;
            }

            if (fileIndex >= _loadedFiles.Count)
            {
                Console.WriteLine($"Index '{fileIndexArgument}' was out of bounds.");
                return;
            }

            var selectedState = _loadedFiles[fileIndex];
            if (!(selectedState.PluginState is ISaveFiles))
            {
                Console.WriteLine($"File '{selectedState.FilePath}' is not savable.");
                return;
            }

            SaveResult saveResult;
            try
            {
                saveResult = await (string.IsNullOrEmpty(savePathArgument)
                    ? PluginManager.SaveFile(selectedState)
                    : PluginManager.SaveFile(selectedState, savePathArgument));
            }
            catch (Exception e)
            {
                Console.WriteLine($"Save Error: {e.Message}");
                return;
            }

            if (!saveResult.IsSuccessful)
            {
                Console.WriteLine($"Save Error: {saveResult.Message}");
                return;
            }

            Console.WriteLine($"Saved '{selectedState.FilePath}' successfully.");
        }

        private void CloseFile(string fileIndexArgument)
        {
            if (!int.TryParse(fileIndexArgument, out var fileIndex))
            {
                Console.WriteLine($"'{fileIndexArgument}' is not a valid number.");
                return;
            }

            if (fileIndex >= _loadedFiles.Count)
            {
                Console.WriteLine($"Index '{fileIndexArgument}' was out of bounds.");
                return;
            }

            var selectedState = _loadedFiles[fileIndex];
            var selectedFile = selectedState.FilePath;

            PluginManager.Close(selectedState);
            _loadedFiles.Remove(selectedState);

            Console.WriteLine($"Closed '{selectedFile}' successfully.");
        }

        private void CloseAll()
        {
            PluginManager.CloseAll();
            _loadedFiles.Clear();

            Console.WriteLine($"Closed all files successfully.");
        }

        private IContext SelectFile(string fileIndexArgument)
        {
            if (!int.TryParse(fileIndexArgument, out var fileIndex))
            {
                Console.WriteLine($"'{fileIndexArgument}' is not a valid number.");
                return this;
            }

            if (fileIndex >= _loadedFiles.Count)
            {
                Console.WriteLine($"Index '{fileIndexArgument}' was out of bounds.");
                return this;
            }

            var selectedState = _loadedFiles[fileIndex];

            Console.WriteLine($"Selected '{selectedState.FilePath}'.");

            return CreateFileContext(selectedState);
        }

        private void ListOpenFiles()
        {
            if (_loadedFiles.Count <= 0)
            {
                Console.WriteLine("No files are open.");
                return;
            }

            for (var i = 0; i < _loadedFiles.Count; i++)
            {
                var loadedFile = _loadedFiles[i];
                Console.WriteLine($"[{i}] {loadedFile.FilePath.GetName()} - {loadedFile.FilePlugin.Metadata.Name} - {loadedFile.FilePlugin.PluginId}");
            }
        }

        private IContext CreateFileContext(IStateInfo stateInfo)
        {
            switch (stateInfo.PluginState)
            {
                case ITextState _:
                    return new TextContext(stateInfo, this);

                case IImageState _:
                    return new ImageContext(stateInfo, this);

                case IArchiveState _:
                    return new ArchiveContext(stateInfo, this, PluginManager);

                default:
                    Console.WriteLine($"State '{stateInfo.PluginState.GetType()}' is not supported.");
                    return null;
            }
        }
    }
}
