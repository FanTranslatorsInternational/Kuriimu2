using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Plugin.File.Image;
using plugin_most_wanted_ent.Compression;

namespace plugin_most_wanted_ent.Images
{
    class CtgdState : ILoadFiles, ISaveFiles, IImageFilePluginState
    {
        private readonly Ctgd _img = new();

        private bool _wasCompressed;
        private List<IImageFile> _images;

        public IReadOnlyList<IImageFile> Images => _images;

        public bool ContentChanged => IsContentChanged();

        public async Task Load(IFileSystem fileSystem, UPath filePath, LoadContext loadContext)
        {
            var fileStream = await fileSystem.OpenFileAsync(filePath);

            // Decompress, if necessary
            using var br = new BinaryReaderX(fileStream, true);
            fileStream.Position = 9;
            if (br.ReadString(4) == "nns_")
            {
                _wasCompressed = true;

                fileStream.Position = 0;

                var decompressedStream = new MemoryStream();
                NintendoCompressor.Decompress(fileStream, decompressedStream);

                fileStream.Close();
                fileStream = decompressedStream;
            }

            fileStream.Position = 0;
            _images = [new ImageFile(_img.Load(fileStream), CtgdSupport.GetEncodingDefinition())];
        }

        public Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            var fileStream = _wasCompressed ? new MemoryStream() : fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);
            _img.Save(fileStream, _images[0].ImageInfo);

            // Compress, if necessary
            if (_wasCompressed)
            {
                var compressedStream = fileSystem.OpenFile(savePath, FileMode.Create, FileAccess.Write);

                fileStream.Position = 0;
                NintendoCompressor.Compress(fileStream, compressedStream, NintendoCompressionMethod.Lz10);
            }

            return Task.CompletedTask;
        }

        private bool IsContentChanged()
        {
            return _images.Any(x => x.ImageInfo.ContentChanged);
        }
    }
}
