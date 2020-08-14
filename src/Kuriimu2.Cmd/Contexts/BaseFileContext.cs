using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kontract;
using Kontract.Extensions;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kore.Managers.Plugins;

namespace Kuriimu2.Cmd.Contexts
{
    abstract class BaseFileContext : BaseContext
    {
        private readonly ContextNode _contextNode;

        protected IInternalPluginManager PluginManager { get; }

        public BaseFileContext(IInternalPluginManager pluginManager)
        {
            PluginManager = pluginManager;

            _contextNode = new ContextNode();
        }

        public BaseFileContext(IInternalPluginManager pluginManager, ContextNode parentContextNode)
        {
            PluginManager = pluginManager;

            _contextNode = parentContextNode;
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

                case "save-all":
                    await SaveAll();
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

            var newNode = _contextNode.Add(loadResult.LoadedState);

            Console.WriteLine($"Loaded '{fileArgument}' successfully.");

            return CreateFileContext(newNode);
        }

        private Task SaveFile(string fileIndexArgument, string savePathArgument)
        {
            if (!int.TryParse(fileIndexArgument, out var fileIndex))
            {
                Console.WriteLine($"'{fileIndexArgument}' is not a valid number.");
                return Task.CompletedTask;
            }

            if (fileIndex >= _contextNode.Children.Count)
            {
                Console.WriteLine($"Index '{fileIndexArgument}' was out of bounds.");
                return Task.CompletedTask;
            }

            return SaveFileInternal(fileIndex, savePathArgument);
        }

        private async Task SaveAll()
        {
            for (var i = 0; i < _contextNode.Children.Count; i++)
            {
                if (_contextNode.Children[i].StateInfo.StateChanged)
                    await SaveFileInternal(i, null);
            }
        }

        private async Task SaveFileInternal(int fileIndex, string savePathArgument)
        {
            var selectedState = _contextNode.Children[fileIndex].StateInfo;
            if (!(selectedState.PluginState is ISaveFiles))
            {
                Console.WriteLine($"File '{selectedState.FilePath}' is not savable.");
                return;
            }

            if (!selectedState.StateChanged)
            {
                Console.WriteLine($"File '{selectedState.FilePath.ToRelative()}' has no changes.");
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

            Console.WriteLine($"Saved '{selectedState.FilePath.ToRelative()}' successfully.");
        }

        private void CloseFile(string fileIndexArgument)
        {
            if (!int.TryParse(fileIndexArgument, out var fileIndex))
            {
                Console.WriteLine($"'{fileIndexArgument}' is not a valid number.");
                return;
            }

            if (fileIndex >= _contextNode.Children.Count)
            {
                Console.WriteLine($"Index '{fileIndexArgument}' was out of bounds.");
                return;
            }

            var selectedState = _contextNode.Children[fileIndex].StateInfo;
            var selectedFile = selectedState.FilePath;

            PluginManager.Close(selectedState);
            _contextNode.Children[fileIndex].Remove();

            Console.WriteLine($"Closed '{selectedFile}' successfully.");
        }

        protected void CloseAll()
        {
            foreach (var child in _contextNode.Children)
                PluginManager.Close(child.StateInfo);

            _contextNode.Children.Clear();

            Console.WriteLine("Closed all files successfully.");
        }

        private IContext SelectFile(string fileIndexArgument)
        {
            if (!int.TryParse(fileIndexArgument, out var fileIndex))
            {
                Console.WriteLine($"'{fileIndexArgument}' is not a valid number.");
                return this;
            }

            if (fileIndex >= _contextNode.Children.Count)
            {
                Console.WriteLine($"Index '{fileIndexArgument}' was out of bounds.");
                return this;
            }

            var selectedNode = _contextNode.Children[fileIndex];

            Console.WriteLine($"Selected '{selectedNode.StateInfo.FilePath.ToRelative()}'.");

            return CreateFileContext(selectedNode);
        }

        private void ListOpenFiles()
        {
            if (_contextNode.Children.Count <= 0)
            {
                Console.WriteLine("No files are open.");
                return;
            }

            _contextNode.ListFiles();
        }

        private IContext CreateFileContext(ContextNode childNode)
        {
            switch (childNode.StateInfo.PluginState)
            {
                case ITextState _:
                    return new TextContext(childNode.StateInfo, this);

                case IImageState _:
                    return new ImageContext(childNode.StateInfo, this);

                case IArchiveState _:
                    return new ArchiveContext(childNode, this, PluginManager);

                default:
                    Console.WriteLine($"State '{childNode.StateInfo.PluginState.GetType()}' is not supported.");
                    return this;
            }
        }
    }

    class ContextNode
    {
        private ContextNode _parentNode;

        public IStateInfo StateInfo { get; }

        public IList<ContextNode> Children { get; }

        public ContextNode()
        {
            Children = new List<ContextNode>();
        }

        private ContextNode(ContextNode parentNode, IStateInfo parentState) : this()
        {
            ContractAssertions.IsNotNull(parentNode, nameof(parentNode));
            ContractAssertions.IsNotNull(parentState, nameof(parentState));

            _parentNode = parentNode;
            StateInfo = parentState;
        }

        public ContextNode Add(IStateInfo stateInfo)
        {
            var newNode = new ContextNode(this, stateInfo);
            Children.Add(newNode);

            return newNode;
        }

        public void ListFiles()
        {
            ListFilesInternal();
        }

        public void Remove()
        {
            _parentNode?.Children.Remove(this);
            _parentNode = null;
        }

        private void ListFilesInternal(int iteration = 0)
        {
            var prefix = new string(' ', iteration * 2);

            for (var i = 0; i < Children.Count; i++)
            {
                if (iteration == 0)
                    prefix = $"[{i}] ";

                if (Children[i].StateInfo.StateChanged)
                    prefix += "* ";

                Console.WriteLine(prefix + Children[i].StateInfo.FilePath.ToRelative());

                Children[i].ListFilesInternal(iteration + 1);
            }
        }
    }
}
