﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontract;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Interfaces.Progress;
using Kontract.Models;
using Kontract.Models.IO;
using Kore.Factories;
using Kore.Managers.Plugins;
using MoreLinq.Extensions;

namespace Kuriimu2.Cmd.Contexts
{
    class ArchiveContext : BaseFileContext
    {
        private readonly IFileState _stateInfo;
        private readonly IArchiveState _archiveState;
        private readonly IFileSystem _archiveFileSystem;
        private readonly IContext _parentContext;

        public ArchiveContext(ContextNode contextNode, IContext parentContext, IInternalFileManager pluginManager, IProgressContext progressContext) :
            base(pluginManager, contextNode, progressContext)
        {
            ContractAssertions.IsNotNull(contextNode, nameof(contextNode));
            ContractAssertions.IsNotNull(parentContext, nameof(parentContext));

            _stateInfo = contextNode.StateInfo;
            _archiveState = _stateInfo.PluginState as IArchiveState;
            _archiveFileSystem = FileSystemFactory.CreateAfiFileSystem(_stateInfo);
            _parentContext = parentContext;
        }

        protected override IList<Command> InitializeCommands()
        {
            var baseCommands = base.InitializeCommands();

            baseCommands.Where(x => x.Name == "save-as").ForEach(x => x.Enabled = false);

            return baseCommands.Concat(new List<Command>
            {
                new Command("list"),
                new Command("back"),
                new Command("back-to-main")
            }).ToArray();
        }

        protected override async Task<IContext> ExecuteNextInternal(Command command, IList<string> arguments)
        {
            var executeContext = await base.ExecuteNextInternal(command, arguments);
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
                    return ContextNode.RootContext;
            }

            return null;
        }

        protected override bool FileExists(string filePath)
        {
            return _archiveFileSystem.FileExists(new UPath(filePath).ToAbsolute());
        }

        protected override bool IsLoaded(string filePath)
        {
            var absolutePath = _stateInfo.AbsoluteDirectory / _stateInfo.FilePath / filePath;
            return PluginManager.IsLoaded(absolutePath);
        }

        protected override async Task<LoadResult> LoadFileInternal(string filePath, Guid pluginId)
        {
            var absoluteFilePath = new UPath(filePath).ToAbsolute();
            var selectedAfi = _archiveState.Files.First(x => x.FilePath == absoluteFilePath);

            // If plugin Id is set, try that one first
            if (pluginId != Guid.Empty)
            {
                var loadResult = await PluginManager.LoadFile(_stateInfo, selectedAfi, pluginId);
                if (loadResult.IsSuccessful)
                    return loadResult;
            }

            // Try every preset plugin afterwards
            foreach (var selectedAfiPluginId in selectedAfi.PluginIds ?? Array.Empty<Guid>())
            {
                var loadResult = await PluginManager.LoadFile(_stateInfo, selectedAfi, selectedAfiPluginId);
                if (loadResult.IsSuccessful)
                    return loadResult;
            }

            // Otherwise open it with automatic identification
            return await PluginManager.LoadFile(_stateInfo, selectedAfi);
        }

        private void ListFiles(IFileSystem fileSystem, UPath listPath, int iteration = 0)
        {
            var prefix = new string(' ', iteration * 2);
            Console.WriteLine(prefix + (iteration == 0 ? _stateInfo.FilePath.ToRelative() : listPath.GetName()));

            // Print files
            foreach (var file in fileSystem.EnumeratePaths(listPath, "*", SearchOption.TopDirectoryOnly, SearchTarget.File))
                Console.WriteLine(prefix + "  " + file.GetName());

            // Print directories
            foreach (var dir in fileSystem.EnumeratePaths(listPath, "*", SearchOption.AllDirectories, SearchTarget.Directory))
                if (listPath != dir && listPath == dir.GetDirectory())
                    ListFiles(fileSystem, dir, iteration + 1);
        }
    }
}
