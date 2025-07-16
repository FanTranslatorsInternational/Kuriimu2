using Konnect.Contract.DataClasses.Management.Files;
using Konnect.Contract.Management.Files;
using plugin_level5.Common.Image.Models;

namespace plugin_level5.Common.Image
{
    internal class ImageComposer
    {
        private readonly IPluginFileManager _fileManager;
        private readonly ImageWriterFactory _writerFactory = new();
        private readonly ImageEncoder _imageEncoder = new();

        public ImageComposer(IPluginFileManager fileManager)
        {
            _fileManager = fileManager;
        }

        public async Task Compose(ImageData data, Stream output)
        {
            ImageRawData rawImageData;

            if (data.KtxState is not null)
            {
                rawImageData = await SaveKtx(data);
            }
            else
            {
                rawImageData = _imageEncoder.Encode(data);
            }

            IImageWriter imageWriter = _writerFactory.Create(data.Version.Version);
            imageWriter.Write(rawImageData, output);
        }

        private async Task<ImageRawData> SaveKtx(ImageData imageData)
        {
            SaveStreamResult saveResult = await _fileManager.SaveStream(imageData.KtxState!);
            if (!saveResult.IsSuccessful)
                throw new InvalidOperationException($"Could not save KTX: {saveResult.Reason}");

            Stream imageStream = saveResult.SavedStreams[0].Stream;
            var data = new byte[imageStream.Length];

            var readData = 0;
            while (readData < data.Length)
                readData += await imageStream.ReadAsync(data, readData, Math.Min(2048, data.Length - readData));

            return new ImageRawData
            {
                Version = imageData.Version,
                BitDepth = imageData.Image.ImageInfo.BitDepth,
                Format = 0x2B,
                PaletteBitDepth = imageData.Image.ImageInfo.PaletteBitDepth,
                PaletteFormat = imageData.Image.ImageInfo.PaletteFormat,
                Width = imageData.Image.ImageInfo.ImageSize.Width,
                Height = imageData.Image.ImageInfo.ImageSize.Height,
                LegacyData = imageData.LegacyData,
                Data = data,
                MipMapData = imageData.Image.ImageInfo.MipMapData?.ToArray() ?? [],
                PaletteData = imageData.Image.ImageInfo.PaletteData
            };
        }
    }
}
