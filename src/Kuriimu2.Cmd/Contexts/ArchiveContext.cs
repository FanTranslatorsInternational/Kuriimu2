using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontract;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.IO;
using Kore.Factories;

namespace Kuriimu2.Cmd.Contexts
{
    class ArchiveContext : BaseContext
    {
        private readonly IStateInfo _stateInfo;
        private readonly IContext _parentContext;
        private readonly IMainContext _mainContext;
        private readonly IFileSystem _archiveFileSystem;
        private readonly IList<IStateInfo> _loadedFiles;

        protected override IList<Command> Commands { get; } = new List<Command>
        {
            new Command("open", "file"),
            new Command("open-with", "file", "plugin-id"),
            new Command("save","file-index"),
            new Command("close","file-index"),
            new Command("list"),
            new Command("list-open"),
            new Command("select","file-index"),
            new Command("back"),
            new Command("back-to-main")
        };

        public ArchiveContext(IStateInfo stateInfo, IContext parentContext, IMainContext mainContext)
        {
            ContractAssertions.IsNotNull(stateInfo, nameof(stateInfo));
            ContractAssertions.IsNotNull(parentContext, nameof(parentContext));
            ContractAssertions.IsNotNull(mainContext, nameof(mainContext));

            _stateInfo = stateInfo;
            _parentContext = parentContext;
            _mainContext = mainContext;
            _archiveFileSystem = FileSystemFactory.CreateAfiFileSystem(stateInfo);
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
                    await SaveFile(arguments[0]);
                    return this;

                case "close":
                    CloseFile(arguments[0]);
                    return this;

                case "close-all":
                    CloseAll();
                    return this;

                case "list":
                    Console.WriteLine(_stateInfo.FilePath);
                    ListFiles(_archiveFileSystem, UPath.Root);
                    return this;

                case "list-open":
                    ListOpenFiles();
                    return this;

                case "select":
                    return SelectFile(arguments[0]);

                case "back":
                    return _parentContext;

                case "back-to-main":
                    return _mainContext;
            }

            return null;
        }

        private async Task LoadFile(UPath file, string pluginIdArgument)
        {
            if (!_archiveFileSystem.FileExists(file))
            {
                Console.WriteLine($"File '{file}' does not exist.");
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

            var archiveState = _stateInfo.PluginState as IArchiveState;
            var selectedAfi = archiveState.Files.First(x => x.FilePath == file);

            var stateInfo = await _mainContext.LoadFile(_stateInfo, selectedAfi, pluginId);

            _loadedFiles.Add(stateInfo);
        }

        private Task SaveFile(string fileIndexArgument)
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
            return _mainContext.SaveFile(loadedFile);
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
            _mainContext.CloseFile(loadedFile);

            if (_loadedFiles.Contains(loadedFile))
                _loadedFiles.Remove(loadedFile);
        }

        private void CloseAll()
        {
            foreach(var loadedFile in _loadedFiles)
                _mainContext.CloseFile(loadedFile);

            _loadedFiles.Clear();
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

            var fileContext = ContextFactory.CreateFileContext(_loadedFiles[index], this, _mainContext);
            return fileContext ?? this;
        }

        private void ListOpenFiles()
        {
            for (var i = 0; i < _loadedFiles.Count; i++)
            {
                var loadedFile = _loadedFiles[i];
                Console.WriteLine($"[{i}] {loadedFile.FilePath.GetName()} - {loadedFile.FilePlugin.Metadata.Name} - {loadedFile.FilePlugin.PluginId}");
            }
        }

        private void ListFiles(IFileSystem fileSystem, UPath listPath, int iteration = 1)
        {
            var prefix = new string(' ', iteration * 2);

            // Print files
            foreach (var file in fileSystem.EnumeratePaths(listPath, "*", SearchOption.TopDirectoryOnly, SearchTarget.File))
                Console.WriteLine(prefix + file.GetName());

            // Print directories
            foreach (var dir in fileSystem.EnumeratePaths(listPath, "*", SearchOption.TopDirectoryOnly,
                SearchTarget.Directory))
            {
                Console.WriteLine(prefix + dir.GetName());
                ListFiles(fileSystem, dir, iteration + 1);
            }
        }
    }
}
