using System.Drawing;
using System.IO;
using Kanvas.Swizzle;
using Kanvas.Swizzle.Models;
using Komponent.IO;
using Komponent.IO.Streams;
using Kompression.Implementations;
using Kontract.Models.Image;
using plugin_inti_creates.Cryptography;

namespace plugin_inti_creates.Images
{
    class Osb
    {
        private OsbHeader _header;
        private byte[] _nodeRegion;
        private byte[] _postData;

        public ImageInfo Load(Stream input, Platform platform)
        {
            input = new IntiCreatesCipherStream(input, "obj90210");

            var f = File.OpenWrite(@"D:\Users\Kirito\Desktop\t.bin");
            input.Position = 0;
            input.CopyTo(f);
            f.Close();

            // Decompress ZLib data
            var ms = new MemoryStream();
            Compressions.ZLib.Build().Decompress(new SubStream(input, 4, input.Length - 4), ms);

            using var br = new BinaryReaderX(ms);

            // Read header
            br.BaseStream.Position = 0;
            _header = br.ReadType<OsbHeader>();

            // Read node region
            br.BaseStream.Position = _header.nodeOffset;
            _nodeRegion = br.ReadBytes(_header.dataOffset - _header.nodeOffset);

            // Read image data
            br.BaseStream.Position = _header.dataOffset;
            var imgData = br.ReadBytes(_header.dataSize);

            // Read post data
            br.BaseStream.Position = _header.postOffset;
            _postData = br.ReadBytes(_header.postSize);

            // Create image info
            var imageInfo = new ImageInfo(imgData, _header.format, new Size(_header.width, _header.height));

            if (platform == Platform.N3DS)
                imageInfo.RemapPixels.With(context => new CtrSwizzle(context, CtrTransformation.YFlip));

            return imageInfo;
        }

        public void Save(Stream output, ImageInfo imageInfo)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriterX(ms);

            // Calculate offsets
            var nodeOffset = _header.nodeOffset;
            var dataOffset = nodeOffset + _nodeRegion.Length;
            var postOffset = dataOffset + imageInfo.ImageData.Length;

            // Write data regions
            ms.Position = nodeOffset;
            bw.Write(_nodeRegion);

            ms.Position = dataOffset;
            bw.Write(imageInfo.ImageData);

            ms.Position = postOffset;
            bw.Write(_postData);

            // Update header
            _header.nodeOffset = nodeOffset;

            _header.dataOffset = dataOffset;
            _header.dataSize = imageInfo.ImageData.Length;

            _header.postOffset = postOffset;
            _header.postSize = _postData.Length;

            _header.format = imageInfo.ImageFormat;
            _header.width = imageInfo.ImageSize.Width;
            _header.height = imageInfo.ImageSize.Height;

            // Write header
            ms.Position = 0;
            bw.WriteType(_header);

            // Compress with ZLib
            output = new IntiCreatesCipherStream(output, "obj90210");
            using var outBw = new BinaryWriterX(output);

            ms.Position = 0;
            using var compStream = new MemoryStream();

            Compressions.ZLib.Build().Compress(ms, compStream);

            // Write compressed data
            output.Position = 0;
            outBw.Write((int)ms.Length);

            compStream.Position = 0;
            compStream.CopyTo(output);
        }
    }
}
