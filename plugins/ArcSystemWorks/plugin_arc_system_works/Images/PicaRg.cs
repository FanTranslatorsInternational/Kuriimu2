using System.Drawing;
using System.IO;
using Kanvas.Swizzle;
using Komponent.IO;
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
            var imageInfo = new ImageInfo(imageData, _header.format, new Size(_header.paddedWidth, _header.paddedHeight));
            imageInfo.RemapPixels.With(context => new CtrSwizzle(context));
            imageInfo.PadSize.ToPowerOfTwo();

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
            // TODO: Point transformation via Kanvas
            _header.format = (ushort)imageInfo.ImageFormat;
            _header.paddedWidth = (short)imageInfo.ImageSize.Width;
            _header.paddedHeight = (short)imageInfo.ImageSize.Height;

            output.Position = 0;
            bw.WriteType(_header);
        }
    }
}
