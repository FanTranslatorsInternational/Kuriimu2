using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Plugin.File.Image;

namespace plugin_sony.Images
{
    class GxtState : ILoadFiles, ISaveFiles, IImageFilePluginState
    {
        private readonly Gxt _gxt = new();
        private List<IImageFile> _images;

        public IReadOnlyList<IImageFile> Images => _images;

        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            _images = _gxt.Load(fileStream).Select(IImageFile (x) => new ImageFile(x, GxtSupport.GetEncodingDefinition())).ToList();
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream output = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);
            _gxt.Save(output, _images.Select(x => x.ImageInfo).ToArray());
        }

        private bool IsContentChanged()
        {
            return _images.Any(x => x.ImageInfo.ContentChanged);
        }
    }
}
