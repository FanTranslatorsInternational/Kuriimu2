using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.FileSystem.EventArgs;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.IO;
using Kore.Managers.Plugins;
using Serilog;

namespace Kore.Batch
{
    public class BatchExtractor : BaseBatchProcessor
    {
        private readonly object _lock = new object();
        private readonly IList<UPath> _openedFiles = new List<UPath>();

        public BatchExtractor(IInternalFileManager fileManager, ILogger logger) :
            base(fileManager, logger)
        {
        }

        protected override async Task ProcessInternal(IFileSystem sourceFileSystem, UPath filePath, IFileSystem destinationFileSystem)
        {
            Logger.Information("Extract '{0}'.", filePath.FullName);

            IFileState loadedFileState;
            lock (_lock)
            {
                _openedFiles.Clear();

                // Load file
                SourceFileSystemWatcher.Opened += SourceFileSystemWatcher_Opened;
                loadedFileState = LoadFile(sourceFileSystem, filePath).Result;
                SourceFileSystemWatcher.Opened -= SourceFileSystemWatcher_Opened;

                // If file could not be loaded successfully
                if (loadedFileState == null)
                    return;

                // If one of the opened files was already batched, stop execution
                if (_openedFiles.Any(IsFileBatched))
                {
                    FileManager.Close(loadedFileState);

                    Logger.Information("'{0}' is/was already processed.", filePath.FullName);
                    return;
                }

                // Add opened files to batched files
                foreach (var openedFile in _openedFiles)
                    AddBatchedFile(openedFile);
            }

            switch (loadedFileState.PluginState)
            {
                case IArchiveState archiveState:
                    await ExtractArchive(archiveState, loadedFileState.FilePath, destinationFileSystem, filePath);
                    break;

                case IImageState imageState:
                    ExtractImage(imageState, destinationFileSystem, filePath);
                    break;

                default:
                    Logger.Error("'{0}' is not supported.", filePath.FullName);
                    FileManager.Close(loadedFileState);
                    return;
            }

            FileManager.Close(loadedFileState);

            Logger.Information("Extracted '{0}'.", filePath.FullName);
        }

        private void SourceFileSystemWatcher_Opened(object sender, FileOpenedEventArgs e)
        {
            _openedFiles.Add(e.OpenedPath);
        }

        private async Task ExtractArchive(IArchiveState archiveState, UPath originalFilepath, IFileSystem destinationFileSystem, UPath filePath)
        {
            if (archiveState.Files.Count > 0)
                CreateDirectory(destinationFileSystem, filePath);

            foreach (var afi in archiveState.Files)
            {
                var systemPath = filePath / afi.FilePath.ToRelative();
                Stream newFileStream = null;

                try
                {
                    newFileStream = destinationFileSystem.OpenFile(systemPath, FileMode.Create, FileAccess.Write);
                    (await afi.GetFileData()).CopyTo(newFileStream);
                }
                catch (Exception e)
                {
                    Logger.Fatal(e, "Unexpected error extracting {0}.", originalFilepath);
                }
                finally
                {
                    newFileStream?.Close();
                }
            }
        }

        private void ExtractImage(IImageState imageState, IFileSystem destinationFileSystem, UPath filePath)
        {
            if (imageState.Images.Count > 0)
                CreateDirectory(destinationFileSystem, filePath);

            var index = 0;
            foreach (var img in imageState.Images)
            {
                var fileStream = destinationFileSystem.OpenFile(filePath / (img.Name ?? $"{index:00}") + ".png", FileMode.Create, FileAccess.Write);
                img.GetImage().Save(fileStream, ImageFormat.Png);

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
