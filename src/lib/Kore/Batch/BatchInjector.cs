using System.Drawing;
using System.Threading.Tasks;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models.FileSystem;
using Kore.Managers.Plugins;
using Serilog;

namespace Kore.Batch
{
    public class BatchInjector : BaseBatchProcessor
    {
        public BatchInjector(IKoreFileManager fileManager, ILogger logger) : 
            base(fileManager, logger)
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
                    InjectArchive(archiveState, filePath, destinationFileSystem);
                    break;

                case IImageState imageState:
                    InjectImages(imageState, filePath, destinationFileSystem);
                    break;

                default:
                    Logger.Error("{0} is not supported.", filePath.FullName);
                    return;
            }

            // Save file with all changes
            await SaveFile(loadedFile);

            // Close file
            FileManager.Close(loadedFile);
        }

        private void InjectArchive(IArchiveState archiveState, UPath filePath, IFileSystem destinationFileSystem)
        {
            foreach (var afi in archiveState.Files)
            {
                var path = filePath / afi.FilePath.ToRelative();
                if (!destinationFileSystem.FileExists(path))
                    continue;

                var afiFileStream = destinationFileSystem.OpenFile(path);
                afi.SetFileData(afiFileStream);
            }
        }

        private void InjectImages(IImageState imageState, UPath filePath, IFileSystem destinationFileSystem)
        {
            for (var i = 0; i < imageState.Images.Count; i++)
            {
                var img = imageState.Images[i];

                var path = filePath / (img.Name ?? $"{i:00}") + ".png";
                if (!destinationFileSystem.FileExists(path))
                    continue;

                var openedImage = (Bitmap)Image.FromStream(destinationFileSystem.OpenFile(path));
                img.SetImage(openedImage);
            }
        }
    }
}
