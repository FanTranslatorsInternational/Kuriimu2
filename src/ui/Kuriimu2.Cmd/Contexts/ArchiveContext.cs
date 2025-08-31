using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Management.Files;
using Konnect.Contract.Enums.FileSystem;
using Konnect.Contract.Enums.Management.Files;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Contract.Progress;
using Konnect.Extensions;
using Konnect.FileSystem;
using Kuriimu2.Cmd.Models.Contexts;

namespace Kuriimu2.Cmd.Contexts
{
    class ArchiveContext : BaseFileContext
    {
        private readonly IFileState _stateInfo;
        private readonly IArchiveFilePluginState _archiveState;
        private readonly IFileSystem _archiveFileSystem;
        private readonly IContext _parentContext;

        public ArchiveContext(ContextNode node, IContext parentContext, IFileManager fileManager, IProgressContext progressContext) :
            base(fileManager, node, progressContext)
        {
            _stateInfo = node.StateInfo!;
            _archiveState = _stateInfo.PluginState.Archive!;
            _archiveFileSystem = FileSystemFactory.CreateArchivePluginFileSystem(_stateInfo);
            _parentContext = parentContext;
        }

        protected override Command[] GetCommandsInternal()
        {
            Command[] baseCommands = base.GetCommandsInternal();

            if (_stateInfo.ParentFileState is not null)
            {
                Command? saveAsCommand = baseCommands.FirstOrDefault(x => x.Name is "save-as");

                if (saveAsCommand is not null)
                    saveAsCommand.Enabled = false;
            }

            return baseCommands.Concat([
                new Command("list"),
                new Command("back"),
                new Command("back-to-main")
            ]).ToArray();
        }

        protected override async Task<IContext?> ExecuteNextInternal(Command command, IList<string> arguments)
        {
            IContext? executeContext = await base.ExecuteNextInternal(command, arguments);
            if (executeContext != null)
                return executeContext;

            switch (command.Name)
            {
                case "list":
                    ListFiles(_archiveFileSystem, UPath.Root);
                    return this;

                case "back":
                    return _parentContext;

                case "back-to-main":
                    return Node.Root;
            }

            return null;
        }

        protected override bool FileExists(string filePath)
        {
            return _archiveFileSystem.FileExists(new UPath(filePath).ToAbsolute());
        }

        protected override bool IsLoaded(string filePath, out IFileState? loadedFile)
        {
            loadedFile = null;

            UPath absolutePath = _stateInfo.AbsoluteDirectory / _stateInfo.FilePath / filePath;
            if (!FileManager.IsLoaded(absolutePath))
                return false;

            loadedFile = FileManager.GetLoadedFile(absolutePath);
            return true;
        }

        protected override async Task<LoadResult> LoadFileInternal(string filePath, Guid pluginId)
        {
            UPath absoluteFilePath = new UPath(filePath).ToAbsolute();
            IArchiveFile selectedAfi = _archiveState.Files.First(x => x.FilePath == absoluteFilePath);

            // If plugin Id is set, try that one first
            if (pluginId != Guid.Empty)
            {
                LoadResult loadResult = await FileManager.LoadFile(_stateInfo, selectedAfi, pluginId);
                
                if (loadResult.Status is LoadStatus.Successful)
                    return loadResult;
            }

            // Try every preset plugin afterwards
            foreach (Guid selectedAfiPluginId in selectedAfi.PluginIds ?? [])
            {
                LoadResult loadResult = await FileManager.LoadFile(_stateInfo, selectedAfi, selectedAfiPluginId);

                if (loadResult.Status is LoadStatus.Successful)
                    return loadResult;
            }

            // Otherwise open it with automatic identification
            return await FileManager.LoadFile(_stateInfo, selectedAfi);
        }

        private void ListFiles(IFileSystem fileSystem, UPath listPath, int iteration = 0)
        {
            var prefix = new string(' ', iteration * 2);
            Console.WriteLine(prefix + (iteration == 0 ? _stateInfo.FilePath.ToRelative() : listPath.GetName()));

            // Print files
            foreach (UPath file in fileSystem.EnumeratePaths(listPath, "*", SearchOption.TopDirectoryOnly, SearchTarget.File))
                Console.WriteLine(prefix + "  " + file.GetName());

            // Print directories
            foreach (UPath dir in fileSystem.EnumeratePaths(listPath, "*", SearchOption.AllDirectories, SearchTarget.Directory))
            {
                if (listPath != dir && listPath == dir.GetDirectory())
                    ListFiles(fileSystem, dir, iteration + 1);
            }
        }
    }
}
