using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Plugin.File.Image;

namespace plugin_yuusha_shisu.Images
{
    public class BtxState : ILoadFiles, ISaveFiles, IImageFilePluginState
    {
        private readonly BTX _btx = new();
        private List<IImageFile> _images;

        public IReadOnlyList<IImageFile> Images => _images;

        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            _images = [new ImageFile(_btx.Load(fileStream), BtxSupport.GetEncodingDefinition())];
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);
            _btx.Save(fileStream, _images[0].ImageInfo);
        }

        private bool IsContentChanged()
        {
            return _images.Any(x => x.ImageInfo.ContentChanged);
        }
    }
}
