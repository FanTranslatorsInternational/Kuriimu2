using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Image;

namespace plugin_cattle_call.Images
{
    class ChnkState : ILoadFiles, IImageFilePluginState
    {
        private readonly Chnk _img = new();
        private List<IImageFile> _images;

        public IReadOnlyList<IImageFile> Images => _images;

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            _images = _img.Load(fileStream);
        }
    }
}
