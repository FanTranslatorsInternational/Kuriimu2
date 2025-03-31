using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_nintendo.Images
{
    class GcBnr
    {
        private const int TitleInfoSize_ = 0x140;

        private GcBnrHeader _header;
        private IList<GcBnrTitleInfo> _titleInfos;

        public ImageFileInfo Load(Stream input)
        {
            var typeReader = new BinaryTypeReader();
            using var br = new BinaryReaderX(input);

            // Read header
            _header = typeReader.Read<GcBnrHeader>(br);

            // Read image data
            var imageData = br.ReadBytes(0x1800);

            // Read title info
            var titleInfoCount = (int)(input.Length - input.Position) / TitleInfoSize_;
            _titleInfos = typeReader.ReadMany<GcBnrTitleInfo>(br, titleInfoCount);

            var imageInfo = new ImageFileInfo
            {
                BitDepth = 16,
                ImageData = imageData,
                ImageFormat = 0,
                ImageSize = new Size(96, 32),
                RemapPixels = context => new DolphinSwizzle(context)
            };

            return imageInfo;
        }

        public void Save(Stream output, ImageFileInfo imageInfo)
        {
            var typeWriter = new BinaryTypeWriter();
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var imageDataOffset = 0x20;
            var titleInfoOffset = imageDataOffset + 0x1800;

            // Write title info
            output.Position = titleInfoOffset;
            typeWriter.WriteMany(_titleInfos, bw);

            // Write image data
            output.Position = imageDataOffset;
            output.Write(imageInfo.ImageData);

            // Write header
            output.Position = 0;
            typeWriter.Write(_header, bw);
        }
    }
}
