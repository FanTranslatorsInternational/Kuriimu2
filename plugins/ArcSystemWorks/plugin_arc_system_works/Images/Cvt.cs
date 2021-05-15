using System.Drawing;
using System.IO;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Models.Image;

namespace plugin_arc_system_works.Images
{
    class Cvt
    {
        private CvtHeader _header;

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<CvtHeader>();

            // Create image info
            input.Position = 0x50;
            var imageData = br.ReadBytes((int)input.Length - 0x50);

            var imageInfo = new ImageInfo(imageData, _header.format, new Size(_header.width, _header.height))
            {
                Name = _header.name.Trim('\0')
            };
            imageInfo.RemapPixels.With(context => new CtrSwizzle(context));

            return imageInfo;
        }

        public void Save(Stream output, ImageInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            // Write image data
            output.Position = 0x50;
            output.Write(imageInfo.ImageData);

            // Update header
            _header.format = (short)imageInfo.ImageFormat;
            _header.width = (short)imageInfo.ImageSize.Width;
            _header.height = (short)imageInfo.ImageSize.Height;

            // Write header
            output.Position = 0;
            bw.WriteType(_header);
        }
    }
}
