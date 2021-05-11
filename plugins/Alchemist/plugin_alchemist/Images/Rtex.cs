using System.Drawing;
using System.IO;
using Kanvas.Swizzle;
using Kanvas.Swizzle.Models;
using Komponent.IO;
using Komponent.IO.Streams;
using Kompression.Implementations;
using Kontract.Models.Image;

namespace plugin_alchemist.Images
{
    class Rtex
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(RtexHeader));
        private static readonly int DataHeaderSize = Tools.MeasureType(typeof(RtexDataHeader));

        private RtexHeader _header;

        public ImageInfo Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<RtexHeader>();

            // Decompress image data
            var decompStream = new MemoryStream();
            var compStream = new SubStream(input, _header.dataOffset + DataHeaderSize, _header.dataSize - DataHeaderSize);
            Compressions.ZLib.Build().Decompress(compStream, decompStream);

            // Create image info
            var imageInfo = new ImageInfo(decompStream.ToArray(), _header.format, new Size(_header.width, _header.height));
            imageInfo.PadSize.ToMultiple(8);

            imageInfo.RemapPixels.With(context => new CtrSwizzle(context, CtrTransformation.YFlip));

            return imageInfo;
        }

        public void Save(Stream output, ImageInfo imageInfo)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var dataHeaderOffset = HeaderSize;
            var dataOffset = dataHeaderOffset + DataHeaderSize;

            // Compress image data
            output.Position = dataOffset;
            Compressions.ZLib.Build().Compress(new MemoryStream(imageInfo.ImageData), output);

            // Write data header
            output.Position = dataHeaderOffset;
            bw.WriteType(new RtexDataHeader { decompSize = imageInfo.ImageData.Length });

            // Update header
            var paddedSize = imageInfo.PadSize.Build(imageInfo.ImageSize);

            _header.dataOffset = dataHeaderOffset;
            _header.dataSize = (int)output.Length - dataHeaderOffset;
            _header.format = (byte)imageInfo.ImageFormat;
            _header.width = (short)imageInfo.ImageSize.Width;
            _header.height = (short)imageInfo.ImageSize.Height;
            _header.paddedWidth = (short)paddedSize.Width;
            _header.paddedHeight = (short)paddedSize.Height;

            // Write header
            output.Position = 0;
            bw.WriteType(_header);
        }
    }
}
