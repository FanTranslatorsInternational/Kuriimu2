using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File;
using Konnect.Contract.Enums.Management.Files;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File;
using Konnect.Contract.Plugin.File.Image;
using plugin_level5.Common.Compression;
using plugin_level5.Common.Image;
using plugin_level5.Common.Image.Models;
using Konnect.Contract.DataClasses.Management.Files;

namespace plugin_level5.Common.Plugins
{
    class ImgState : IImageFilePluginState, ILoadFiles, ISaveFiles
    {
        private static readonly Guid KtxPluginId = Guid.Parse("d25919cc-ac22-4f4a-94b2-b0f42d1123d4");

        private readonly ImageVersionReader _versionReader = new();
        private readonly ImageReaderFactory _readerFactory = new();
        private readonly ImageWriterFactory _writerFactory = new();
        private readonly ImageEncoder _encoder = new();
        private readonly Decompressor _decompressor = new();
        private readonly Compressor _compressor = new();

        private readonly IPluginFileManager _fileManager;

        private ImageRawData? _ktxRawData;
        private IFileState? _ktxState;
        private Level5CompressionMethod? _ktxCompressionMethod;

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

            int version = _versionReader.Peek(fileStream);
            IImageReader imageReader = _readerFactory.Create(version);

            ImageRawData rawData = imageReader.Read(fileStream);

            if (rawData.Version.Platform is PlatformType.Android && rawData.Format is 0x2B)
            {
                // Handle KTX
                _ktxRawData = rawData;

                IImageFilePluginState imageState = await LoadKtx(rawData.Data);
                _images = [imageState.Images[0]];
            }
            else
            {
                ImageDecoder decoder = new(loadContext.DialogManager!);

                _imgData = await decoder.Decode(rawData);
                _images = [_imgData.Image];
            }
        }

        public async Task Save(IFileSystem fileSystem, UPath savePath, SaveContext saveContext)
        {
            Stream fileStream = await fileSystem.OpenFileAsync(savePath, FileMode.Create, FileAccess.Write);

            ImageRawData rawData;

            if (_ktxState is not null)
            {
                // Handle KTX
                _ktxRawData!.Data = await SaveKtx();

                rawData = _ktxRawData;
            }
            else
            {
                rawData = _encoder.Encode(_imgData!);
            }

            IImageWriter imageWriter = _writerFactory.Create(rawData.Version.Version);
            imageWriter.Write(rawData, fileStream);
        }

        private async Task<IImageFilePluginState> LoadKtx(byte[] data)
        {
            using var dataStream = new MemoryStream(data);
            _ktxCompressionMethod = _decompressor.PeekCompressionType(dataStream, 0);

            var ktxFile = new MemoryStream();
            _decompressor.Decompress(dataStream, ktxFile, 0);

            var file = new StreamFile
            {
                Stream = ktxFile,
                Path = "image.ktx"
            };

            LoadResult loadResult = await _fileManager.LoadFile(file, KtxPluginId);
            if (loadResult.Status is not LoadStatus.Successful)
                throw new InvalidOperationException($"{loadResult.Reason}");
            if (loadResult.LoadedFileState?.PluginState is not IImageFilePluginState)
                throw new InvalidOperationException("The embedded KTX version is not supported.");

            _ktxState = loadResult.LoadedFileState;
            return (IImageFilePluginState)_ktxState.PluginState;
        }

        private async Task<byte[]> SaveKtx()
        {
            SaveStreamResult saveResult = await _fileManager.SaveStream(_ktxState!);
            if (!saveResult.IsSuccessful)
                throw new InvalidOperationException($"Could not save KTX: {saveResult.Reason}");

            _ktxRawData!.BitDepth = _images[0].ImageInfo.BitDepth;
            _ktxRawData!.Format = _images[0].ImageInfo.ImageFormat;
            _ktxRawData!.Width = _images[0].ImageInfo.ImageSize.Width;
            _ktxRawData!.Height = _images[0].ImageInfo.ImageSize.Height;

            _ktxRawData!.PaletteBitDepth = _images[0].ImageInfo.PaletteBitDepth;
            _ktxRawData!.PaletteData = _images[0].ImageInfo.PaletteData;
            _ktxRawData!.PaletteFormat = _images[0].ImageInfo.PaletteFormat;

            _ktxRawData!.MipMapData = _images[0].ImageInfo.MipMapData?.ToArray() ?? [];

            MemoryStream dataStream = _compressor.Compress(saveResult.SavedStreams[0].Stream, _ktxCompressionMethod!.Value);
            return dataStream.ToArray();
        }
    }
}
