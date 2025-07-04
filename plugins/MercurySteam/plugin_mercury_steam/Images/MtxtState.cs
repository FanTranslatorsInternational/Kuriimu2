using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Image;

namespace plugin_mercury_steam.Images
{
    class MtxtState(IPluginFileManager fileManager) : ILoadFiles, ISaveFiles, IImageFilePluginState
    {
        private readonly Mtxt _mtxt = new(fileManager);

        public IReadOnlyList<IImageFile> Images { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            Images = await _mtxt.Load(fileStream);
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);
            await _mtxt.Save(fileStream);
        }

        private bool IsContentChanged()
        {
            return Images.Any(x => x.ImageInfo.ContentChanged);
        }
    }
}
