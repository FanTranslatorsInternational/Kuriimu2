using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kontract;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Models;
using Kontract.Models.IO;
using Kore.Managers.Plugins;
using Serilog;

namespace Kore.Batch
{
    public abstract class BaseBatchProcessor
    {
        private int _processedFiles;
        private HashSet<UPath> _batchedFiles;
        private CancellationTokenSource _processTokenSource;

        protected IInternalFileManager FileManager { get; }
        protected ILogger Logger { get; }
        protected IFileSystemWatcher SourceFileSystemWatcher { get; private set; }

        public bool ScanSubDirectories { get; set; }

        public Guid PluginId { get; set; }

        public TimeSpan AverageFileTime { get; private set; }

        public BaseBatchProcessor(IInternalFileManager fileManager, ILogger logger)
        {
            ContractAssertions.IsNotNull(fileManager, nameof(fileManager));
            ContractAssertions.IsNotNull(logger, nameof(logger));

            FileManager = fileManager;
            Logger = logger;
        }

        public async Task Process(IFileSystem sourceFileSystem, IFileSystem destinationFileSystem)
        {
            _processedFiles = 0;
            _batchedFiles = new HashSet<UPath>();

            SourceFileSystemWatcher = sourceFileSystem.Watch(UPath.Root);

            var files = EnumerateFiles(sourceFileSystem).ToArray();

            var isManualSelection = FileManager.AllowManualSelection;
            FileManager.AllowManualSelection = false;

            await ProcessMeasurement(files, sourceFileSystem, destinationFileSystem);

            FileManager.AllowManualSelection = isManualSelection;

            SourceFileSystemWatcher.Dispose();
        }

        public void Cancel()
        {
            _processTokenSource?.Cancel();
        }

        protected abstract Task ProcessInternal(IFileSystem sourceFileSystem, UPath filePath, IFileSystem destinationFileSystem);

        protected bool IsFileBatched(UPath filePath)
        {
            return _batchedFiles.Any(x => x == filePath);
        }

        protected void AddBatchedFile(UPath filePath)
        {
            _batchedFiles.Add(filePath);
        }

        protected async Task<IFileState> LoadFile(IFileSystem sourceFileSystem, UPath filePath)
        {
            LoadResult loadResult;
            try
            {
                loadResult = PluginId == Guid.Empty ?
                    await FileManager.LoadFile(sourceFileSystem, filePath) :
                    await FileManager.LoadFile(sourceFileSystem, filePath, PluginId);
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "Loading file '{0}' threw an error.", filePath.FullName);
                return null;
            }

            if (loadResult.IsSuccessful)
                return loadResult.LoadedFileState;

            Logger.Error("Could not load '{0}'.", filePath.FullName);
            return null;
        }

        protected async Task SaveFile(IFileState fileState)
        {
            SaveResult saveResult;
            try
            {
                saveResult = await FileManager.SaveFile(fileState);
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "Saving file '{0}' threw an error.", fileState.FilePath.FullName);
                return;
            }

            if (!saveResult.IsSuccessful)
                Logger.Error("Could not save '{0}'.", fileState.FilePath.FullName);
        }

        private async Task ProcessMeasurement(UPath[] files, IFileSystem sourceFs, IFileSystem destinationFs)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            _processTokenSource = new CancellationTokenSource();
            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        files.AsParallel().WithCancellation(_processTokenSource.Token)
                            .ForAll(async x =>
                            {
                                if (_processTokenSource.Token.IsCancellationRequested)
                                    return;

                                await ProcessInternal(sourceFs, x, destinationFs);

                                var processed = _processedFiles + 1;
                                _processedFiles++;

                                AverageFileTime = TimeSpan.FromTicks(stopwatch.ElapsedTicks / processed);
                            });
                    }
                    catch (OperationCanceledException)
                    {

                    }
                }, _processTokenSource.Token);
            }
            finally
            {
                stopwatch.Stop();
            }
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
