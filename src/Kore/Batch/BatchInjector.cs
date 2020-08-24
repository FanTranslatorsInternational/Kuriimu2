using System.Drawing;
using System.Threading.Tasks;
using Kanvas;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Logging;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.IO;
using Kontract.Models.Logging;
using Kore.Managers.Plugins;

namespace Kore.Batch
{
    class BatchInjector : BaseBatchProcessor
    {
        public BatchInjector(IInternalPluginManager pluginManager, IConcurrentLogger logger) :
            base(pluginManager, logger)
        {
        }

        protected override async Task ProcessInternal(IFileSystem sourceFileSystem, UPath filePath, IFileSystem destinationFileSystem)
        {
            if (!destinationFileSystem.DirectoryExists(filePath))
                return;

            // Load file
            var loadedFile = await LoadFile(sourceFileSystem, filePath);
            if (loadedFile == null)
                return;

            switch (loadedFile.PluginState)
            {
                case IArchiveState archiveState:
                    foreach (var afi in archiveState.Files)
                    {
                        var path = filePath / afi.FilePath.ToRelative();
                        if (!destinationFileSystem.FileExists(path))
                            continue;

                        var afiFileStream = destinationFileSystem.OpenFile(path);
                        afi.SetFileData(afiFileStream);

                        afiFileStream.Close();
                    }
                    break;

                case IImageState imageState:
                    var index = 0;
                    foreach (var img in imageState.Images)
                    {
                        var path = filePath / (img.Name ?? $"{index:00}") + ".png";
                        if (!destinationFileSystem.FileExists(path))
                            continue;

                        // TODO: Can we get the reference to Kanvas out of here?
                        var kanvasImage = new KanvasImage(imageState, img);

                        var openedImage = (Bitmap)Image.FromStream(destinationFileSystem.OpenFile(path));
                        kanvasImage.SetImage(openedImage);

                        index++;
                    }
                    break;

                default:
                    Logger.QueueMessage(LogLevel.Error, $"{loadedFile.PluginState.GetType().Name}' is not supported.");
                    return;
            }

            // Save file with all changes
            await SaveFile(loadedFile);

            // Close file
            PluginManager.Close(loadedFile);
        }
    }
}
