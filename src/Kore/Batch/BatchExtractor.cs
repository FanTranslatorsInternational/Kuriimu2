using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.FileSystem.EventArgs;
using Kontract.Interfaces.Logging;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.IO;
using Kontract.Models.Logging;
using Kore.Managers.Plugins;

namespace Kore.Batch
{
    public class BatchExtractor : BaseBatchProcessor
    {
        private readonly object _lock = new object();
        private readonly IList<UPath> _openedFiles = new List<UPath>();

        public BatchExtractor(IInternalPluginManager pluginManager, IConcurrentLogger logger) :
            base(pluginManager, logger)
        {
        }

        protected override async Task ProcessInternal(IFileSystem sourceFileSystem, UPath filePath, IFileSystem destinationFileSystem)
        {
            Logger.QueueMessage(LogLevel.Information, $"Extract '{filePath}'.");

            IStateInfo loadedState;
            lock (_lock)
            {
                _openedFiles.Clear();

                // Load file
                SourceFileSystemWatcher.Opened += SourceFileSystemWatcher_Opened;
                loadedState = LoadFile(sourceFileSystem, filePath).Result;
                SourceFileSystemWatcher.Opened -= SourceFileSystemWatcher_Opened;

                // If one of the opened files was already batched, stop execution
                if (_openedFiles.Any(IsFileBatched))
                {
                    PluginManager.Close(loadedState);

                    Logger.QueueMessage(LogLevel.Information, $"'{filePath}' is/was already processed.");
                    return;
                }

                // Add opened files to batched files
                foreach (var openedFile in _openedFiles)
                    AddBatchedFile(openedFile);
            }

            switch (loadedState.PluginState)
            {
                case IArchiveState archiveState:
                    await ExtractArchive(archiveState, destinationFileSystem, filePath);
                    break;

                case IImageState imageState:
                    ExtractImage(imageState, destinationFileSystem, filePath);
                    break;

                default:
                    Logger.QueueMessage(LogLevel.Error, $"'{filePath}' is not supported.");
                    PluginManager.Close(loadedState);
                    return;
            }

            PluginManager.Close(loadedState);

            Logger.QueueMessage(LogLevel.Information, $"Extracted '{filePath}'.");
        }

        private void SourceFileSystemWatcher_Opened(object sender, FileOpenedEventArgs e)
        {
            _openedFiles.Add(e.OpenedPath);
        }

        private async Task ExtractArchive(IArchiveState archiveState, IFileSystem destinationFileSystem, UPath filePath)
        {
            if (archiveState.Files.Count > 0)
                CreateDirectory(destinationFileSystem, filePath);

            foreach (var afi in archiveState.Files)
            {
                var newFileStream = destinationFileSystem.OpenFile(filePath / afi.FilePath.ToRelative(), FileMode.Create, FileAccess.Write);
                (await afi.GetFileData()).CopyTo(newFileStream);

                newFileStream.Close();
            }
        }

        private void ExtractImage(IImageState imageState, IFileSystem destinationFileSystem, UPath filePath)
        {
            if (imageState.Images.Count > 0)
                CreateDirectory(destinationFileSystem, filePath);

            var index = 0;
            foreach (var img in imageState.Images)
            {
                var imgStream = new MemoryStream();
                img.GetImage().Save(imgStream, ImageFormat.Png);

                var fileStream = destinationFileSystem.OpenFile(filePath / (img.Name ?? $"{index:00}") + ".png", FileMode.Create, FileAccess.Write);
                imgStream.Position = 0;
                imgStream.CopyTo(fileStream);

                fileStream.Close();

                index++;
            }
        }

        private void CreateDirectory(IFileSystem fileSystem, UPath path)
        {
            if (!fileSystem.DirectoryExists(path))
                fileSystem.CreateDirectory(path);
        }
    }
}
