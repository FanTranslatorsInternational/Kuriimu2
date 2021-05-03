using System.Drawing;
using System.IO;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Models.Image;

namespace plugin_ganbarion.Images
{
    class Jtex
    {
        private JtexHeader _header;
        private byte[] _unkRegion;

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<JtexHeader>();

            // Read unknown region
            _unkRegion = br.ReadBytes(_header.dataOffset - (int)br.BaseStream.Position);

            // Create image info
            input.Position = _header.dataOffset;
            var imageData = br.ReadBytes(_header.dataSize);

            var imageInfo = new ImageInfo(imageData, _header.format, new Size(_header.width, _header.height));
            imageInfo.PadSize.ToPowerOfTwo();
            imageInfo.RemapPixels.With(context => new CtrSwizzle(context));

            return imageInfo;
        }

        public void Save(Stream output, ImageInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            var paddedSize = imageInfo.PadSize.Build(imageInfo.ImageSize);

            // Write image data
            output.Position = _header.dataOffset;
            output.Write(imageInfo.ImageData);

            // Update header
            _header.fileSize = (int)output.Length;
            _header.width = (short)paddedSize.Width;
            _header.height = (short)paddedSize.Height;
            _header.format = (byte)imageInfo.ImageFormat;
            _header.dataSize = imageInfo.ImageData.Length;

            // Write header
            output.Position = 0;
            bw.WriteType(_header);

            // Write unknown region
            bw.Write(_unkRegion);
        }
    }
}
