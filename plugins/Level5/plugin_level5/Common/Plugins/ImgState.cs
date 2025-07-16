using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Image;
using plugin_level5.Common.Compression;
using plugin_level5.Common.Image;
using plugin_level5.Common.Image.Models;

namespace plugin_level5.Common.Plugins
{
    class ImgState : ILoadFiles, ISaveFiles, IImageFilePluginState
    {
        private readonly ImageWriterFactory _writerFactory = new();
        private readonly ImageEncoder _encoder = new();
        private readonly Compressor _compressor = new();

        private readonly IPluginFileManager _fileManager;

        private ImageData? _imgData;

        private List<IImageFile> _images;

        public IReadOnlyList<IImageFile> Images => _images;
        public bool ContentChanged => _images.Any(x => x.ImageInfo.ContentChanged);

        public ImgState(IPluginFileManager fileManager)
        {
            _fileManager = fileManager;
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);

            var imageParser = new ImageParser(loadContext.DialogManager!, _fileManager);
            _imgData = await imageParser.Parse(fileStream);

            _images = [_imgData.Image];
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);

            var imageComposer = new ImageComposer(_fileManager);
            await imageComposer.Compose(_imgData!, fileStream);
        }
    }
}
