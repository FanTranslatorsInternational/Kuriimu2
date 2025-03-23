using plugin_level5.Common.Image.Models;

namespace plugin_level5.Common.Image
{
    internal class ImageComposer
    {
        private readonly ImageWriterFactory _writerFactory = new();
        private readonly ImageEncoder _imageEncoder = new();

        public void Compose(ImageData data, Stream output)
        {
            ImageRawData rawImageData = _imageEncoder.Encode(data);

            IImageWriter imageWriter = _writerFactory.Create(data.Version.Version);
            imageWriter.Write(rawImageData, output);
        }
    }
}
