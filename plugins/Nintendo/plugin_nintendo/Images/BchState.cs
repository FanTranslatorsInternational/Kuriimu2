using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Plugin.File.Image;

namespace plugin_nintendo.Images
{
    class BchState : IImageFilePluginState, ILoadFiles, ISaveFiles
    {
        private readonly Bch _bch = new();

        private List<ImageFileInfo> _imageInfos;

        public IReadOnlyList<IImageFile> Images { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            _imageInfos = _bch.Load(fileStream);
            Images = _imageInfos.Select(x => new ImageFile(x, true, BchSupport.GetEncodingDefinition())).ToArray();
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);
            _bch.Save(fileStream, _imageInfos);
        }

        private bool IsContentChanged()
        {
            return Images.Any(x => x.ImageInfo.ContentChanged);
        }
    }
}
