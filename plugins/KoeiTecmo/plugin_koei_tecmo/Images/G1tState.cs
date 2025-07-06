using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Plugin.File.Image;

namespace plugin_koei_tecmo.Images
{
    class G1tState : ILoadFiles, ISaveFiles, IImageFilePluginState
    {
        private readonly G1t _img = new();
        private List<IImageFile> _images;

        public IReadOnlyList<IImageFile> Images => _images;

        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            var platform = G1tSupport.DeterminePlatform(fileStream, loadContext.DialogManager);
            var encodingDefinition = G1tSupport.GetEncodingDefinition(platform);

            fileStream.Position = 0;
            _images = _img.Load(fileStream, platform).Select(IImageFile (x) => new ImageFile(x, encodingDefinition)).ToList();
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);
            _img.Save(fileStream, _images.Select(x => x.ImageInfo).ToArray());
        }

        private bool IsContentChanged()
        {
            return _images.Any(x => x.ImageInfo.ContentChanged);
        }
    }
}
