using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Plugin.File.Image;

namespace plugin_nintendo.Images
{
    class BimgState : IImageFilePluginState, ILoadFiles, ISaveFiles
    {
        private readonly Bimg _img = new();

        private ImageFileInfo _imageInfo;

        public IReadOnlyList<IImageFile> Images { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            _imageInfo = _img.Load(fileStream);
            Images = [new ImageFile(_imageInfo, BimgSupport.GetEncodingDefinition())];
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);
            _img.Save(fileStream, _imageInfo);
        }

        private bool IsContentChanged()
        {
            return _imageInfo.ContentChanged;
        }
    }
}
