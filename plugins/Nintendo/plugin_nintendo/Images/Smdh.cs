using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Kanvas.Swizzle;
using Komponent.IO;
using Kontract.Models.Image;

namespace plugin_nintendo.Images
{
    class Smdh
    {
        private static readonly int HeaderSize = Tools.MeasureType(typeof(SmdhHeader));
        private static readonly int AppTitleSize = Tools.MeasureType(typeof(SmdhApplicationTitle));
        private static readonly int AppSettingsSize = Tools.MeasureType(typeof(SmdhAppSettings));

        private SmdhHeader _header;
        private IList<SmdhApplicationTitle> _appTitles;
        private SmdhAppSettings _settings;

        public IList<ImageInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input);

            // Read header
            _header = br.ReadType<SmdhHeader>();

            // Read application titles
            _appTitles = br.ReadMultiple<SmdhApplicationTitle>(0x10);

            // Read application settings
            _settings = br.ReadType<SmdhAppSettings>();
            br.BaseStream.Position += 0x8;

            // Read image data
            var result = new List<ImageInfo>();

            var imageData = br.ReadBytes(0x480);
            result.Add(new ImageInfo(imageData, 0, new Size(24, 24)));
            result.Last().RemapPixels.With(context => new CtrSwizzle(context));

            imageData = br.ReadBytes(0x1200);
            result.Add(new ImageInfo(imageData, 0, new Size(48, 48)));
            result.Last().RemapPixels.With(context => new CtrSwizzle(context));

            return result;
        }

        public void Save(Stream output, IList<ImageInfo> imageInfos)
        {
            using var bw = new BinaryWriterX(output);

            // Calculate offsets
            var dataOffset = (HeaderSize + _appTitles.Count * AppTitleSize + AppSettingsSize + 0xF) & ~0xF;

            // Write image data
            output.Position = dataOffset;
            foreach (var imageInfo in imageInfos.OrderBy(x => x.ImageSize.Width))
                bw.Write(imageInfo.ImageData);

            // Write icon information
            output.Position = 0;
            bw.WriteType(_header);
            bw.WriteMultiple(_appTitles);
            bw.WriteType(_settings);
        }
    }
}
