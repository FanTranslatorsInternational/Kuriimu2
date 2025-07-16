using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Management.Files;
using Konnect.Contract.Enums.Management.Files;
using Konnect.Contract.Management.Dialog;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File.Image;
using plugin_level5.Common.Image.Models;

namespace plugin_level5.Common.Image
{
    internal class ImageParser
    {
        private static readonly Guid KtxPluginId = Guid.Parse("d25919cc-ac22-4f4a-94b2-b0f42d1123d4");

        private readonly IPluginFileManager _fileManager;
        private readonly ImageVersionReader _versionReader = new();
        private readonly ImageReaderFactory _readerFactory = new();
        private readonly ImageDecoder _imageDecoder;

        public ImageParser(IDialogManager dialogManager, IPluginFileManager fileManager)
        {
            _imageDecoder = new ImageDecoder(dialogManager);
            _fileManager = fileManager;
        }

        public async Task<ImageData> Parse(Stream input)
        {
            int imageVersion = _versionReader.Peek(input);

            IImageReader imageReader = _readerFactory.Create(imageVersion);
            ImageRawData imageData = imageReader.Read(input);

            if (imageData.Version.Platform is not PlatformType.Android || imageData.Format is not 0x2B)
                return await _imageDecoder.Decode(imageData);

            // Handle KTX
            IFileState? ktxState = await LoadKtx(imageData.Data);

            if (ktxState?.PluginState is not IImageFilePluginState imageState)
                throw new InvalidOperationException("The embedded KTX version is not supported.");

            return new ImageData
            {
                Version = imageData.Version,
                Image = imageState.Images[0],
                LegacyData = null,
                KtxState = ktxState
            };

        }

        private async Task<IFileState?> LoadKtx(byte[] data)
        {
            var file = new StreamFile
            {
                Stream = new MemoryStream(data),
                Path = "image.ktx"
            };

            LoadResult loadResult = await _fileManager.LoadFile(file, KtxPluginId);
            if (loadResult.Status is not LoadStatus.Successful)
                throw new InvalidOperationException($"{loadResult.Reason}");

            return loadResult.LoadedFileState;
        }
    }
}
