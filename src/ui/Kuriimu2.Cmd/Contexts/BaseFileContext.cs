using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Management.Files;
using Konnect.Contract.DataClasses.Plugin;
using Konnect.Contract.Enums.Management.Files;
using Konnect.Contract.Exceptions.Management.Files;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Management.Plugin;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Font;
using Konnect.Contract.Plugin.File.Hex;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Contract.Plugin.File.Text;
using Konnect.Contract.Progress;
using Konnect.Extensions;
using Kuriimu2.Cmd.Models.Contexts;

namespace Kuriimu2.Cmd.Contexts
{
    abstract class BaseFileContext : BaseContext
    {
        protected IFileManager FileManager { get; }

        protected IPluginManager PluginManager { get; }

        protected ContextNode Node { get; }

        protected BaseFileContext(IPluginManager pluginManager, IFileManager fileManager, IProgressContext progressContext) :
            base(progressContext)
        {
            FileManager = fileManager;
            PluginManager = pluginManager;

            Node = new ContextNode(this);
        }

        protected BaseFileContext(IFileManager fileManager, ContextNode parentNode, IProgressContext progressContext) :
            base(progressContext)
        {
            FileManager = fileManager;

            Node = parentNode;
        }

        protected override async Task<IContext?> ExecuteNextInternal(Command command, IList<string> arguments)
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
                    await Save();
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

        protected override Command[] GetCommandsInternal()
        {
            return
            [
                new Command("open", "file"),
                new Command("open-with", "file", "plugin-id"),
                new Command("save", "file-index"),
                new Command("save-as", "file-index", "save-path"),
                new Command("save-all"),
                new Command("save-this"),
                new Command("close", "file-index"),
                new Command("close-all"),
                new Command("select", "file-index"),
                new Command("list-open")
            ];
        }

        protected abstract bool FileExists(string filePath);

        protected abstract bool IsLoaded(string filePath, out IFileState? loadedFile);

        #region Load

        protected abstract Task<LoadResult> LoadFileInternal(string filePath, Guid pluginId);

        private async Task<IContext?> LoadFile(string fileArgument, string? pluginIdArgument)
        {
            if (!FileExists(fileArgument))
            {
                Console.WriteLine($"File '{fileArgument}' does not exist.");
                return this;
            }

            if (IsLoaded(fileArgument, out IFileState? loadedFile))
            {
                Console.WriteLine($"File '{fileArgument}' already loaded.");
                return loadedFile is null ? this : Node.GetLoadedContext(loadedFile);
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

            if (loadResult.Status is not LoadStatus.Successful)
            {
                if (loadResult.Reason is LoadErrorReason.Deprecated)
                {
                    if (loadResult.Exception is not FilePluginDeprecatedException deprecatedException)
                    {
                        Console.WriteLine("Plugin is deprecated.");
                        return this;
                    }

                    Console.WriteLine($"Plugin '{deprecatedException.Plugin.Metadata.Name}' is deprecated.");

                    if (deprecatedException.Plugin.Alternatives.Length <= 0)
                        return this;

                    Console.WriteLine();
                    Console.WriteLine("Consider using one of the following alternatives instead:");

                    foreach (DeprecatedPluginAlternative alternative in deprecatedException.Plugin.Alternatives)
                        Console.WriteLine($"{alternative.ToolName}: {alternative.Url}");

                    return this;
                }

                Console.WriteLine($"Load Error: {loadResult.Reason}");
                return this;
            }

            if (loadResult.LoadedFileState!.PluginState is IHexFilePluginState)
            {
                Console.WriteLine("No plugin supports this file.");
                return this;
            }

            Console.WriteLine($"Loaded '{fileArgument}' successfully.");

            IContext context = CreateFileContext(loadResult.LoadedFileState);
            Node.Add(context, loadResult.LoadedFileState);

            return context;
        }

        #endregion

        #region Save

        private async Task SaveFile(string fileIndexArgument, string? savePathArgument)
        {
            if (!int.TryParse(fileIndexArgument, out int fileIndex))
            {
                Console.WriteLine($"'{fileIndexArgument}' is not a valid number.");
            }

            if (fileIndex < 0 || fileIndex >= Node.Children.Count)
            {
                Console.WriteLine($"Index '{fileIndexArgument}' was out of bounds.");
            }

            await SaveFileInternal(fileIndex, savePathArgument);
        }

        private async Task SaveAll()
        {
            for (var i = 0; i < Node.Children.Count; i++)
            {
                if (Node.Children[i].StateInfo is null)
                    continue;

                if (Node.Children[i].StateInfo!.StateChanged)
                    await SaveFileInternal(i, null);
            }
        }

        private async Task Save()
        {
            IFileState? selectedState = Node.StateInfo;
            await SaveFileInternal(selectedState, null);
        }

        private async Task SaveFileInternal(int fileIndex, string? savePathArgument)
        {
            IFileState? selectedState = Node.Children[fileIndex].StateInfo;
            await SaveFileInternal(selectedState, savePathArgument);
        }

        private async Task SaveFileInternal(IFileState? selectedState, string? savePathArgument)
        {
            if (selectedState is null)
            {
                Console.WriteLine("No file state was set.");
                return;
            }

            if (!selectedState.PluginState.CanSave)
            {
                Console.WriteLine($"File '{selectedState.FilePath}' is not savable.");
                return;
            }

            SaveResult saveResult;
            try
            {
                saveResult = await (string.IsNullOrEmpty(savePathArgument)
                    ? FileManager.SaveFile(selectedState)
                    : FileManager.SaveFile(selectedState, savePathArgument));
            }
            catch (Exception e)
            {
                Console.WriteLine($"Save Error: {e.Message}");
                return;
            }

            if (!saveResult.IsSuccessful)
            {

                if (saveResult.Reason is SaveErrorReason.NoChanges)
                {
                    Console.WriteLine($"File '{selectedState.FilePath.ToRelative()}' has no changes.");
                    return;
                }

                Console.WriteLine($"Save Error: {saveResult.Reason}");
                return;
            }

            Console.WriteLine($"Saved '{selectedState.FilePath.ToRelative()}' successfully.");
        }

        #endregion

        #region Close

        private void CloseFile(string fileIndexArgument)
        {
            if (!int.TryParse(fileIndexArgument, out int fileIndex))
            {
                Console.WriteLine($"'{fileIndexArgument}' is not a valid number.");
                return;
            }

            if (fileIndex < 0 || fileIndex >= Node.Children.Count)
            {
                Console.WriteLine($"Index '{fileIndexArgument}' was out of bounds.");
                return;
            }

            IFileState? selectedState = Node.Children[fileIndex].StateInfo;
            if (selectedState is null)
            {
                Console.WriteLine("No state was set.");
                return;
            }

            UPath selectedFile = selectedState.FilePath;

            CloseResult closeResult = FileManager.Close(selectedState);
            if (!closeResult.IsSuccessful)
            {
                Console.WriteLine($"Close Error: {closeResult.Reason}");
                return;
            }

            Node.Children[fileIndex].Remove();

            Console.WriteLine($"Closed '{selectedFile}' successfully.");
        }

        protected void CloseAll()
        {
            ContextNode[] children = Node.Children.ToArray();

            if (children.Length <= 0)
                return;

            var allSuccessful = true;
            foreach (ContextNode child in children)
            {
                if (child.StateInfo is null)
                    continue;

                CloseResult closeResult = FileManager.Close(child.StateInfo);
                if (closeResult.IsSuccessful)
                    Node.Children.Remove(child);

                allSuccessful &= closeResult.IsSuccessful;
            }

            Console.WriteLine(allSuccessful
                ? "Closed all files successfully."
                : "Some files could not be closed successfully.");
        }

        #endregion

        private IContext SelectFile(string fileIndexArgument)
        {
            if (!int.TryParse(fileIndexArgument, out int fileIndex))
            {
                Console.WriteLine($"'{fileIndexArgument}' is not a valid number.");
                return this;
            }

            if (fileIndex < 0 || fileIndex >= Node.Children.Count)
            {
                Console.WriteLine($"Index '{fileIndexArgument}' was out of bounds.");
                return this;
            }

            ContextNode selectedNode = Node.Children[fileIndex];
            if (selectedNode.StateInfo is null)
            {
                Console.WriteLine("No file state was set.");
                return this;
            }

            Console.WriteLine($"Selected '{selectedNode.StateInfo.FilePath.ToRelative()}'.");

            return CreateFileContext(selectedNode.StateInfo);
        }

        private void ListOpenFiles()
        {
            if (Node.Children.Count <= 0)
            {
                Console.WriteLine("No files are open.");
                return;
            }

            Node.ListFiles();
        }

        private IContext CreateFileContext(IFileState? loadedFile)
        {
            if (loadedFile is null)
            {
                Console.WriteLine("No file state was set.");
                return this;
            }

            switch (loadedFile.PluginState)
            {
                case ITextFilePluginState:
                    return new TextContext(loadedFile, this, PluginManager, Progress);

                case IImageFilePluginState:
                    return new ImageContext(loadedFile, this, Progress);

                case IArchiveFilePluginState:
                    return new ArchiveContext(Node, this, FileManager, Progress);

                case IFontFilePluginState:
                    return new FontContext(loadedFile, this, Progress);

                default:
                    Console.WriteLine($"State '{loadedFile.PluginState.GetType()}' is not supported.");
                    return this;
            }
        }
    }
}
