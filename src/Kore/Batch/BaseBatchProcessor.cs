using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontract;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Logging;
using Kontract.Interfaces.Managers;
using Kontract.Models;
using Kontract.Models.IO;
using Kontract.Models.Logging;
using Kore.Managers.Plugins;

namespace Kore.Batch
{
    abstract class BaseBatchProcessor
    {
        protected IInternalPluginManager PluginManager { get; }
        protected IConcurrentLogger Logger { get; }

        public bool ScanSubDirectories { get; set; }

        public Guid PluginId { get; set; }

        public BaseBatchProcessor(IInternalPluginManager pluginManager, IConcurrentLogger logger)
        {
            ContractAssertions.IsNotNull(pluginManager, nameof(pluginManager));
            ContractAssertions.IsNotNull(logger, nameof(logger));

            PluginManager = pluginManager;
            Logger = logger;
        }

        public async Task Process(IFileSystem sourceFileSystem, IFileSystem destinationFileSystem)
        {
            var files = EnumerateFiles(sourceFileSystem).ToArray();

            Logger.StartLogging();
            await Task.Run(() => files.AsParallel().ForAll(async x => await ProcessInternal(sourceFileSystem, x, destinationFileSystem)));
            Logger.StopLogging();
        }

        protected abstract Task ProcessInternal(IFileSystem sourceFileSystem, UPath filePath, IFileSystem destinationFileSystem);

        protected async Task<IStateInfo> LoadFile(IFileSystem sourceFileSystem, UPath filePath)
        {
            LoadResult loadResult;
            try
            {
                loadResult = PluginId == Guid.Empty ?
                    await PluginManager.LoadFile(sourceFileSystem, filePath) :
                    await PluginManager.LoadFile(sourceFileSystem, filePath, PluginId);
            }
            catch (Exception e)
            {
                Logger.QueueMessage(LogLevel.Error, $"Load error: {e.Message}");
                return null;
            }

            if (loadResult.IsSuccessful)
                return loadResult.LoadedState;

            Logger.QueueMessage(LogLevel.Error, $"Could not load '{filePath}'.");
            return null;
        }

        protected async Task SaveFile(IStateInfo stateInfo)
        {
            SaveResult saveResult;
            try
            {
                saveResult = await PluginManager.SaveFile(stateInfo);
            }
            catch (Exception e)
            {
                Logger.QueueMessage(LogLevel.Error, $"Save Error: {e.Message}");
                return;
            }

            if (!saveResult.IsSuccessful)
                Logger.QueueMessage(LogLevel.Error, $"Could not save '{stateInfo.FilePath}'.");
        }

        private IEnumerable<UPath> EnumerateFiles(IFileSystem sourceFileSystem)
        {
            if (ScanSubDirectories)
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
    }
}
