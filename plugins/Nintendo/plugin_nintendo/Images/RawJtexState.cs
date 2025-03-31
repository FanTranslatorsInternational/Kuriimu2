using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Plugin.File.Image;
using plugin_nintendo.Common.Compression;

namespace plugin_nintendo.Images
{
    class RawJtexState : IImageFilePluginState, ILoadFiles, ISaveFiles
    {
        private readonly RawJtex _raw = new();
        private NintendoCompressionMethod? _method;

        private ImageFileInfo _imageInfo;

        public IReadOnlyList<IImageFile> Images { get; private set; }

        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(filePath);
            if (IsCompressed(fileStream))
                _method = NintendoCompressor.PeekCompressionMethod(fileStream);

            if (_method != null)
                fileStream = Decompress(fileStream);

            _imageInfo = _raw.Load(fileStream);

            Images = [new ImageFile(_imageInfo, RawJtexSupport.GetEncodingDefinition())];
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream fileStream = _method != null ?
                new MemoryStream() :
                await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);

            _raw.Save(fileStream, _imageInfo);

            if (_method == null)
                return;

            Stream output = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);

            fileStream.Position = 0;
            NintendoCompressor.Compress(fileStream, output, _method.Value);
        }

        private bool IsContentChanged()
        {
            return Images.Any(x => x.ImageInfo.ContentChanged);
        }

        private bool IsCompressed(Stream input)
        {
            var buffer = new byte[4];
            input.Read(buffer);

            input.Position = 0;
            return (buffer[0] == 0x10 || buffer[0] == 0x11) && (buffer[1] != 0 || buffer[2] != 0 || buffer[3] != 0);
        }

        private Stream Decompress(Stream input)
        {
            var output = new MemoryStream();
            NintendoCompressor.Decompress(input, output);

            output.Position = 0;
            return output;
        }
    }
}
