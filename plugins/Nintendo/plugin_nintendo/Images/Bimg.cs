using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_nintendo.Images
{
    class Bimg
    {
        private const int HeaderSize_ = 0x20;

        private BimgHeader _header;

        public ImageFileInfo Load(Stream input)
        {
            var typeReader = new BinaryTypeReader();
            using var br = new BinaryReaderX(input);

            // Read header
            _header = typeReader.Read<BimgHeader>(br);

            // Read image data
            var imgData = br.ReadBytes(_header.dataSize);

            // Create image info
            var imageInfo = new ImageFileInfo
            {
                BitDepth = BimgSupport.GetEncodingDefinition().GetColorEncoding(_header.format).BitDepth,
                ImageData = imgData,
                ImageFormat = _header.format,
                ImageSize = new Size(_header.width, _header.height),
                RemapPixels = context => new CtrSwizzle(context)
            };

            return imageInfo;
        }

        public void Save(Stream output, ImageFileInfo imageInfo)
        {
            var typeWriter = new BinaryTypeWriter();
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var dataOffset = HeaderSize_;

            // Write image data
            output.Position = dataOffset;
            bw.Write(imageInfo.ImageData);

            // Update header
            _header.format = imageInfo.ImageFormat;
            _header.dataSize = imageInfo.ImageData.Length;
            _header.width = (short)imageInfo.ImageSize.Width;
            _header.height = (short)imageInfo.ImageSize.Height;

            // Write header
            output.Position = 0;
            typeWriter.Write(_header, bw);
        }
    }
}
