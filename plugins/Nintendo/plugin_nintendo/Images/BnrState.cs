using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Plugin.File.Image;

namespace plugin_nintendo.Images
{
    class BnrState : IImageFilePluginState, ILoadFiles, ISaveFiles
    {
        private readonly Bnr _bnr = new();

        private ImageFileInfo _imageInfo;

        public IReadOnlyList<IImageFile> Images { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            _imageInfo = _bnr.Load(fileStream);
            Images = [new ImageFile(_imageInfo, BnrSupport.GetEncodingDefinition())];
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.ReadWrite);
            _bnr.Save(fileStream, Images[0].ImageInfo);
        }

        private bool IsContentChanged()
        {
            return Images.Any(x => x.ImageInfo.ContentChanged);
        }
    }
}
