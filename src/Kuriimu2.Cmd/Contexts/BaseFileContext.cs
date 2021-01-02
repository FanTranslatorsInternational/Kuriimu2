using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Kontract;
using Kontract.Extensions;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Progress;
using Kontract.Models;
using Kore.Managers.Plugins;

namespace Kuriimu2.Cmd.Contexts
{
    abstract class BaseFileContext : BaseContext
    {
        protected IInternalPluginManager PluginManager { get; }

        protected IProgressContext Progress { get; }

        protected ContextNode ContextNode { get; }

        public BaseFileContext(IInternalPluginManager pluginManager, IProgressContext progressContext) :
            base(progressContext)
        {
            ContractAssertions.IsNotNull(progressContext, nameof(progressContext));

            PluginManager = pluginManager;
            Progress = progressContext;

            ContextNode = new ContextNode();
        }

        public BaseFileContext(IInternalPluginManager pluginManager, ContextNode parentContextNode, IProgressContext progressContext) :
            base(progressContext)
        {
            PluginManager = pluginManager;

            ContextNode = parentContextNode;
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

                case "save-this":
                    await SaveThis();
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

            var newNode = ContextNode.Add(this, loadResult.LoadedState);

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

            if (fileIndex >= ContextNode.Children.Count)
            {
                Console.WriteLine($"Index '{fileIndexArgument}' was out of bounds.");
                return Task.CompletedTask;
            }

            return SaveFileInternal(fileIndex, savePathArgument);
        }

        private async Task SaveAll()
        {
            for (var i = 0; i < ContextNode.Children.Count; i++)
            {
                if (ContextNode.Children[i].StateInfo.StateChanged)
                    await SaveFileInternal(i, null);
            }
        }

        private Task SaveThis()
        {
            var selectedState = ContextNode.StateInfo;
            return SaveFileInternal(selectedState, null);
        }

        private Task SaveFileInternal(int fileIndex, string savePathArgument)
        {
            var selectedState = ContextNode.Children[fileIndex].StateInfo;
            return SaveFileInternal(selectedState, savePathArgument);
        }

        private async Task SaveFileInternal(IStateInfo selectedState, string savePathArgument)
        {
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

            if (fileIndex >= ContextNode.Children.Count)
            {
                Console.WriteLine($"Index '{fileIndexArgument}' was out of bounds.");
                return;
            }

            var selectedState = ContextNode.Children[fileIndex].StateInfo;
            var selectedFile = selectedState.FilePath;

            PluginManager.Close(selectedState);
            ContextNode.Children[fileIndex].Remove();

            Console.WriteLine($"Closed '{selectedFile}' successfully.");
        }

        protected void CloseAll()
        {
            foreach (var child in ContextNode.Children)
                PluginManager.Close(child.StateInfo);

            ContextNode.Children.Clear();

            Console.WriteLine("Closed all files successfully.");
        }

        private IContext SelectFile(string fileIndexArgument)
        {
            if (!int.TryParse(fileIndexArgument, out var fileIndex))
            {
                Console.WriteLine($"'{fileIndexArgument}' is not a valid number.");
                return this;
            }

            if (fileIndex >= ContextNode.Children.Count)
            {
                Console.WriteLine($"Index '{fileIndexArgument}' was out of bounds.");
                return this;
            }

            var selectedNode = ContextNode.Children[fileIndex];

            Console.WriteLine($"Selected '{selectedNode.StateInfo.FilePath.ToRelative()}'.");

            return CreateFileContext(selectedNode);
        }

        private void ListOpenFiles()
        {
            if (ContextNode.Children.Count <= 0)
            {
                Console.WriteLine("No files are open.");
                return;
            }

            ContextNode.ListFiles();
        }

        private IContext CreateFileContext(ContextNode childNode)
        {
            switch (childNode.StateInfo.PluginState)
            {
                case ITextState _:
                    return new TextContext(childNode.StateInfo, this, Progress);

                case IImageState _:
                    return new ImageContext(childNode.StateInfo, this, Progress);

                case IArchiveState _:
                    return new ArchiveContext(childNode, this, PluginManager, Progress);

                default:
                    Console.WriteLine($"State '{childNode.StateInfo.PluginState.GetType()}' is not supported.");
                    return this;
            }
        }
    }

    [DebuggerDisplay("{StateInfo.FilePath}")]
    class ContextNode
    {
        private ContextNode _parentNode;
        private IContext _parentContext;

        public IStateInfo StateInfo { get; }

        public IContext RootContext => GetRootContext();

        public IList<ContextNode> Children { get; }

        public ContextNode()
        {
            Children = new List<ContextNode>();
        }

        private ContextNode(IContext parentContext, ContextNode parentNode, IStateInfo parentState) : this()
        {
            ContractAssertions.IsNotNull(parentContext, nameof(parentContext));
            ContractAssertions.IsNotNull(parentNode, nameof(parentNode));
            ContractAssertions.IsNotNull(parentState, nameof(parentState));

            _parentNode = parentNode;
            _parentContext = parentContext;
            StateInfo = parentState;
        }

        public ContextNode Add(IContext parentContext, IStateInfo stateInfo)
        {
            var newNode = new ContextNode(parentContext, this, stateInfo);
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

        private IContext GetRootContext()
        {
            if (_parentNode == null)
                throw new InvalidOperationException("Can't get root context of the root.");

            var currentNode = _parentNode;
            var currentContext = _parentContext;
            while (currentNode._parentContext != null && currentNode._parentNode != null)
            {
                currentContext = currentNode._parentContext;
                currentNode = currentNode._parentNode;
            }

            return currentContext;
        }
    }
}
