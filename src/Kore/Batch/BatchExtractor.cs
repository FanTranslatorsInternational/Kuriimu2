using System;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using Kanvas;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Logging;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.IO;
using Kontract.Models.Logging;
using Kore.Managers.Plugins;

namespace Kore.Batch
{
    public class BatchExtractor : BaseBatchProcessor
    {
        public BatchExtractor(IInternalPluginManager pluginManager, IConcurrentLogger logger) :
            base(pluginManager, logger)
        {
        }

        protected override async Task ProcessInternal(IFileSystem sourceFileSystem, UPath filePath, IFileSystem destinationFileSystem)
        {
            Logger.QueueMessage(LogLevel.Information, $"Extract '{filePath}'.");

            // Load file
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
                return;
            }

            if (!loadResult.IsSuccessful)
            {
                Logger.QueueMessage(LogLevel.Error, $"Could not load '{filePath}'.");
                return;
            }

            // Create directory
            if (!destinationFileSystem.DirectoryExists(filePath))
                destinationFileSystem.CreateDirectory(filePath);

            switch (loadResult.LoadedState.PluginState)
            {
                case IArchiveState archiveState:
                    foreach (var afi in archiveState.Files)
                    {
                        var newFileStream = destinationFileSystem.OpenFile(afi.FilePath, FileMode.Create, FileAccess.Write);
                        (await afi.GetFileData()).CopyTo(newFileStream);

                        newFileStream.Close();
                    }
                    break;

                case IImageState imageState:
                    var index = 0;
                    foreach (var img in imageState.Images)
                    {
                        // TODO: Can we get the reference to Kanvas out of here?
                        var kanvasImage = new KanvasImage(imageState, img);

                        var imgStream = new MemoryStream();
                        kanvasImage.GetImage().Save(imgStream, ImageFormat.Png);

                        var fileStream = destinationFileSystem.OpenFile(filePath / (img.Name ?? $"{index:00}") + ".png");
                        imgStream.Position = 0;
                        imgStream.CopyTo(fileStream);

                        index++;
                    }
                    break;

                default:
                    Logger.QueueMessage(LogLevel.Error, $"'{filePath}' is not supported.");
                    return;
            }

            Logger.QueueMessage(LogLevel.Information, $"Extracted '{filePath}'.");
        }
    }
}
