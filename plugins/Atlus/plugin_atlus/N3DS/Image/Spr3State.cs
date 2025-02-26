using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Plugin.File.Image;

namespace plugin_atlus.N3DS.Image
{
    internal class Spr3State : IImageFilePluginState, ILoadFiles, ISaveFiles
    {
        private Spr3 _img;
        private IPluginFileManager _manager;

        public EncodingDefinition EncodingDefinition { get; }
        private List<ImageFile> _images;

        public IReadOnlyList<IImageFile> Images => _images;

        public bool ContentChanged => _images.Any(x => x.ImageInfo.ContentChanged);

        public Spr3State(IPluginFileManager fileManager)
        {
            _manager = fileManager;
            _img = new Spr3();

            EncodingDefinition = Spr3Support.GetEncodingDefinition();
        }

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);
            _images = (List<ImageFile>)_img.Load(fileStream, _manager);
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);
            _img.Save(fileStream, _manager);

            return Task.CompletedTask;
        }
    }
}
