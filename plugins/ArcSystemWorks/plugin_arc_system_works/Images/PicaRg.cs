using System.Drawing;
using System.IO;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Kanvas.Model;
using Kontract.Models.Image;

namespace plugin_arc_system_works.Images
{
    class PicaRg
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(PicaRgHeader));

        private PicaRgHeader _header;

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<PicaRgHeader>();

            // Read image data
            var imageData = br.ReadBytes((int)(input.Length - HeaderSize));

            // Create image info
            var imageInfo = new ImageInfo(imageData, _header.format, new Size(_header.width, _header.height));
            imageInfo.RemapPixels.With(context => new CtrSwizzle(context));
            imageInfo.PadSize.ToPowerOfTwo();
            imageInfo.IsAnchoredAt = ImageAnchor.BottomLeft;

            return imageInfo;
        }

        public void Save(Stream output, ImageInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var dataOffset = HeaderSize;

            // Write image data
            output.Position = dataOffset;
            output.Write(imageInfo.ImageData);

            // Write header
            _header.format = (ushort)imageInfo.ImageFormat;
            _header.width = (short)imageInfo.ImageSize.Width;
            _header.height = (short)imageInfo.ImageSize.Height;

            var paddedSize = imageInfo.PadSize.Build(imageInfo.ImageSize);
            _header.width = (short)paddedSize.Width;
            _header.height = (short)paddedSize.Height;

            output.Position = 0;
            bw.WriteType(_header);
        }
    }
}
