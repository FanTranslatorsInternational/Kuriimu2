using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Management.Files;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Konnect.Contract.Enums.Management.Files;

namespace Kuriimu2.Cmd.Batch
{
    abstract class BatchProcessor(IFileManager fileManager, ILogger logger)
    {
        private int _processedFiles;
        private HashSet<UPath>? _batchedFiles;
        private CancellationTokenSource? _processTokenSource;

        protected IFileManager FileManager { get; } = fileManager;
        protected ILogger Logger { get; } = logger;
        protected IFileSystemWatcher? SourceFileSystemWatcher { get; private set; }

        public bool ScanSubDirectories { get; set; }

        public IFilePlugin? Plugin { get; set; }

        public TimeSpan AverageFileTime { get; private set; }

        public async Task Process(IFileSystem sourceFileSystem, IFileSystem destinationFileSystem)
        {
            _processedFiles = 0;
            _batchedFiles = [];

            SourceFileSystemWatcher = sourceFileSystem.Watch(UPath.Root);

            // Collect files
            IEnumerable<UPath> fileEnumeration = Array.Empty<UPath>();
            fileEnumeration = Plugin?.FileExtensions is { Length: > 0 } 
                ? Plugin.FileExtensions.Aggregate(fileEnumeration, (current, ext) => current.Concat(sourceFileSystem.EnumerateAllFiles(UPath.Root, ext))) 
                : sourceFileSystem.EnumerateAllFiles(UPath.Root);

            bool isManualSelection = FileManager.AllowManualSelection;
            FileManager.AllowManualSelection = false;

            await ProcessMeasurement(fileEnumeration.ToArray(), sourceFileSystem, destinationFileSystem);

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
            return _batchedFiles is not null && _batchedFiles.Any(x => x == filePath);
        }

        protected void AddBatchedFile(UPath filePath)
        {
            _batchedFiles?.Add(filePath);
        }

        protected async Task<IFileState?> LoadFile(IFileSystem sourceFileSystem, UPath filePath)
        {
            LoadResult loadResult;
            try
            {
                loadResult = Plugin is null || Plugin.PluginId == Guid.Empty ?
                    await FileManager.LoadFile(sourceFileSystem, filePath) :
                    await FileManager.LoadFile(sourceFileSystem, filePath, Plugin.PluginId);
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "Loading file '{0}' threw an error.", filePath.FullName);
                return null;
            }

            if (loadResult.Status is LoadStatus.Successful)
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
                        files.AsParallel().WithDegreeOfParallelism(1).WithCancellation(_processTokenSource.Token)
                            .ForAll(x =>
                            {
                                if (_processTokenSource.Token.IsCancellationRequested)
                                    return;

                                ProcessInternal(sourceFs, x, destinationFs).Wait();

                                int processed = _processedFiles + 1;
                                _processedFiles++;

                                AverageFileTime = TimeSpan.FromTicks(stopwatch.ElapsedTicks / processed);
                            });
                    }
                    catch (OperationCanceledException)
                    { }
                }, _processTokenSource.Token);
            }
            finally
            {
                stopwatch.Stop();
            }
        }
    }
}
