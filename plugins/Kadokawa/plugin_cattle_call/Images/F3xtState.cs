using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Plugin.File.Image;
using plugin_cattle_call.Compression;

namespace plugin_cattle_call.Images
{
    class F3xtState : ILoadFiles, ISaveFiles, IImageFilePluginState
    {
        private readonly F3xt _img = new();
        private List<IImageFile> _images;

        private bool _wasCompressed;
        private NintendoCompressionMethod _compMethod;

        public IReadOnlyList<IImageFile> Images => _images;

        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            fileStream = Decompress(fileStream);

            _images = [new ImageFile(_img.Load(fileStream), F3xtSupport.GetEncodingDefinition())];
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream fileStream = _wasCompressed ? new MemoryStream() : await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);

            _img.Save(fileStream, _images[0].ImageInfo);

            if (_wasCompressed)
            {
                var compStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);

                fileStream.Position = 0;
                NintendoCompressor.Compress(fileStream, compStream, _compMethod);
            }
        }

        private bool IsContentChanged()
        {
            return _images.Any(x => x.ImageInfo.ContentChanged);
        }

        private Stream Decompress(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            input.Position = 5;
            var magic = br.ReadString(4);

            if (magic != "F3XT")
            {
                input.Position = 0;

                _wasCompressed = false;
                return input;
            }

            _wasCompressed = true;

            var ms = new MemoryStream();
            input.Position = 0;

            _compMethod = NintendoCompressor.PeekCompressionMethod(input);
            NintendoCompressor.Decompress(input, ms);

            ms.Position = 0;
            return ms;
        }
    }
}
