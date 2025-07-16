using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Plugin.File.Image;

namespace plugin_shade.Images
{
    public class ShtxState : ILoadFiles, ISaveFiles, IImageFilePluginState
    {
        private readonly SHTX _shtx = new();
        private List<IImageFile> _images;

        public IReadOnlyList<IImageFile> Images => _images;
        public bool ContentChanged => IsChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var encodingDefinition = await ShtxSupport.DetermineFormatMapping(loadContext.DialogManager!);

            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            var img = _shtx.Load(fileStream);

            _images = [new ImageFile(img, encodingDefinition)];
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.ReadWrite);
            _shtx.Save(fileStream, _images[0].ImageInfo);
        }

        private bool IsChanged()
        {
            return _images.Any(x => x.ImageInfo.ContentChanged);
        }
    }
}