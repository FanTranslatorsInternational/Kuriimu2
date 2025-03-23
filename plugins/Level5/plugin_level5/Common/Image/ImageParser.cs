using Konnect.Contract.Management.Dialog;
using plugin_level5.Common.Image.Models;

namespace plugin_level5.Common.Image
{
    internal class ImageParser
    {
        private readonly ImageVersionReader _versionReader = new();
        private readonly ImageReaderFactory _readerFactory = new();
        private readonly ImageDecoder _imageDecoder;

        public ImageParser(IDialogManager dialogManager)
        {
            _imageDecoder = new ImageDecoder(dialogManager);
        }

        public async Task<ImageData> Parse(Stream input)
        {
            int imageVersion = _versionReader.Peek(input);

            IImageReader imageReader = _readerFactory.Create(imageVersion);
            ImageRawData imageData = imageReader.Read(input);

            return await _imageDecoder.Decode(imageData);
        }
    }
}
