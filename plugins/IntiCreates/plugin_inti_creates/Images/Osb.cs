using Kanvas.Contract.Enums.Swizzle;
using Kanvas.Swizzle;
using Komponent.IO;
using Komponent.Streams;
using Kompression;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using plugin_inti_creates.Cryptography;
using SixLabors.ImageSharp;

namespace plugin_inti_creates.Images
{
    class Osb
    {
        private OsbHeader _header;
        private byte[] _nodeRegion;
        private byte[] _postData;

        public ImageFileInfo Load(Stream input, Platform platform)
        {
            input = new IntiCreatesCipherStream(input, "obj90210");

            // Decompress ZLib data
            var ms = new MemoryStream();
            Compressions.ZLib.Build().Decompress(new SubStream(input, 4, input.Length - 4), ms);

            using var br = new BinaryReaderX(ms);

            // Read header
            br.BaseStream.Position = 0;
            _header = ReadHeader(br);

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
            var imageInfo = new ImageFileInfo
            {
                BitDepth = OsbSupport.Formats[_header.format].BitDepth,
                ImageData = imgData,
                ImageFormat = _header.format,
                ImageSize = new Size(_header.width, _header.height)
            };

            if (platform == Platform.N3DS)
                imageInfo.RemapPixels = context => new CtrSwizzle(context, CtrTransformation.YFlip);

            return imageInfo;
        }

        public void Save(Stream output, ImageFileInfo imageInfo)
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
            WriteHeader(_header, bw);

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

        private OsbHeader ReadHeader(BinaryReaderX reader)
        {
            return new OsbHeader
            {
                nodeOffset = reader.ReadInt32(),
                dataSize = reader.ReadInt32(),
                format = reader.ReadInt32(),
                width = reader.ReadInt32(),
                height = reader.ReadInt32(),
                dataOffset = reader.ReadInt32(),
                postSize = reader.ReadInt32(),
                postOffset = reader.ReadInt32(),
                unk1 = reader.ReadInt32(),
                unk2 = reader.ReadInt32(),
                unk3 = reader.ReadInt32()
            };
        }

        private void WriteHeader(OsbHeader header, BinaryWriterX writer)
        {
            writer.Write(header.nodeOffset);
            writer.Write(header.dataSize);
            writer.Write(header.format);
            writer.Write(header.width);
            writer.Write(header.height);
            writer.Write(header.dataOffset);
            writer.Write(header.postSize);
            writer.Write(header.postOffset);
            writer.Write(header.unk1);
            writer.Write(header.unk2);
            writer.Write(header.unk3);
        }
    }
}
