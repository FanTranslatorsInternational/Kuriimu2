using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Image;

namespace plugin_atlus.N3DS.Image
{
    internal class Spr3State : IImageFilePluginState, ILoadFiles, ISaveFiles
    {
        private readonly Spr3 _img = new();
        private readonly IPluginFileManager _fileManager;

        public IReadOnlyList<IImageFile> Images { get; private set; }

        public bool ContentChanged => Images.Any(x => x.ImageInfo.ContentChanged);

        public Spr3State(IPluginFileManager fileFileManager)
        {
            _fileManager = fileFileManager;
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            Images = _img.Load(fileStream, _fileManager);
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);
            _img.Save(fileStream, _fileManager);
        }
    }
}
