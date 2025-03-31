using Kanvas.Swizzle;
using Komponent.IO;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using SixLabors.ImageSharp;

namespace plugin_nintendo.Images
{
    class Smdh
    {
        private const int HeaderSize_ = 0x8;
        private const int AppTitleSize_ = 0x200;
        private const int AppSettingsSize_ = 0x30;

        private SmdhHeader _header;
        private IList<SmdhApplicationTitle> _appTitles;
        private SmdhAppSettings _settings;

        public List<ImageFileInfo> Load(Stream input)
        {
            var typeReader = new BinaryTypeReader();
            using var br = new BinaryReaderX(input);

            // Read header
            _header = typeReader.Read<SmdhHeader>(br);

            // Read application titles
            _appTitles = typeReader.ReadMany<SmdhApplicationTitle>(br, 0x10);

            // Read application settings
            _settings = typeReader.Read<SmdhAppSettings>(br);
            br.BaseStream.Position += 0x8;

            // Read image data
            var result = new List<ImageFileInfo>();

            var imageData = br.ReadBytes(0x480);
            result.Add(new ImageFileInfo
            {
                BitDepth = 16,
                ImageData = imageData,
                ImageFormat = 0,
                ImageSize = new Size(24, 24),
                RemapPixels = context => new CtrSwizzle(context)
            });

            imageData = br.ReadBytes(0x1200);
            result.Add(new ImageFileInfo
            {
                BitDepth = 16,
                ImageData = imageData,
                ImageFormat = 0,
                ImageSize = new Size(48, 48),
                RemapPixels = context => new CtrSwizzle(context)
            });

            return result;
        }

        public void Save(Stream output, List<ImageFileInfo> imageInfos)
        {
            var typeWriter = new BinaryTypeWriter();
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var dataOffset = (HeaderSize_ + _appTitles.Count * AppTitleSize_ + AppSettingsSize_ + 0xF) & ~0xF;

            // Write image data
            output.Position = dataOffset;
            foreach (var imageInfo in imageInfos.OrderBy(x => x.ImageSize.Width))
                bw.Write(imageInfo.ImageData);

            // Write icon information
            output.Position = 0;
            typeWriter.Write(_header, bw);
            typeWriter.WriteMany(_appTitles, bw);
            typeWriter.Write(_settings, bw);
        }
    }
}
